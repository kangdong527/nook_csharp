# ADR-003 컨테이너 배포 구성 — 구현체 선택·마이그레이션 적용·네트워크·공개 시점

- 상태: 채택됨
- 날짜: 2026-11-07
- 결정자: 강동진
- 작성: Claude (사용자 요청, DEP 2단계 세션)
- 관련: `docs/design/dep-plan.md`(단계·결정 1~6), ADR-002(마이그레이션 적용 방식을 이 ADR로 미룸), ADR-009(스토리지 삭제는 커밋 뒤)

## 맥락

DEP 2단계에서 api·web을 컨테이너로 만들고 `deploy/compose.yaml`(caddy → web → api → db)로 묶었다. 이 과정에서 네 가지를 정해야 했다.

1. **구현체 선택**: `Program.cs`는 메일 발송기·이미지 저장소를 `IsDevelopment()`일 때만 등록했다. compose는 운영과 같게 `Production`으로 띄워야 하는데, 그러면 메일 워커가 발송기가 없어 시작에서 실패한다. 실제 구현체(SES·S3)는 5~7단계에 붙는다.
2. **마이그레이션 적용**: ADR-002가 "적용 시점·방식은 ADR-003"으로 미뤘다. 원본은 SQL Editor에 붙여넣어 1회 실행했다(이력 없음).
3. **네트워크 노출**: BFF 결정(FE-2)으로 브라우저는 C# API를 직접 부르지 않는다. 컨테이너 배치에서 이것을 강제할지.
4. **공개 시점**: dep-plan 순서상 5단계(EC2, SG 80·443 열기)가 7단계(IP RateLimiter·메일 상한·ForwardedHeaders)보다 앞선다.

## 결정

| # | 질문 | 결정 | 기각한 대안 |
|---|---|---|---|
| 1 | 바깥 경계 구현체를 무엇으로 고르나 | **설정값** `Email:Provider`(Console)·`ImageStorage:Provider`(LocalDisk). Development만 기본값이 있고, 그 밖의 환경은 값이 없거나 모르는 값이면 **시작에서 실패**(fail-fast). 7단계에서 `Ses`·`S3`를 값 하나로 추가 | (a) compose도 `Development`로 띄우기 — 오류 상세·OpenAPI가 켜져 운영과 다른 것을 검증하게 됨. (b) `Staging` 환경 추가 — 환경 이름에 구현체 선택이 묶여 조합이 늘 때마다 분기 추가 |
| 2 | 마이그레이션은 언제·어떻게 적용하나 | **1회성 컨테이너**(`migrate`): `dotnet ef migrations bundle`로 만든 단일 실행 파일. compose에서 db healthy → migrate 성공(exit 0) → api 시작(`service_completed_successfully`). 연결 문자열은 실행 시 환경변수로만 | (a) 앱 시작 시 `Database.Migrate()` — 인스턴스가 늘면 동시 적용 경합, 실패하면 앱이 안 뜸, 운영 앱에 DDL 권한 필요. (b) 사람이 `dotnet ef database update` — 이력·재현성 없음(원본과 같은 문제) |
| 3 | 무엇을 바깥에 여나 | **Caddy 하나만** 호스트 포트 공개. 네트워크 2개: `edge`(caddy·web), `backend`(web·api·migrate·db, `internal: true`). Caddyfile에 api 경로 없음 → 바깥에서 C# API·DB에 닿는 길이 없다 | api를 Caddy에 `/api/*`로 노출 — BFF를 우회하는 두 번째 입구가 생기고 토큰이 브라우저 JS로 갈 유혹이 생김 |
| 4 | 컨테이너 권한 | 비루트(api = `app` UID 1654, web = `node` UID 1000). **3단계 보강(2026-11-07)**: api·migrate 실행 이미지를 `-noble-chiseled-extra`(셸·apt 없음)로, web 실행 이미지에서 npm·corepack·yarn 삭제 — Trivy HIGH(apt CVE-2026-109518, npm 동봉 undici·brace-expansion) 해소. 대가: api 컨테이너에서 `exec sh` 불가. `read_only` 루트 파일시스템 + 필요한 곳만 tmpfs, `cap_drop: ALL`, `no-new-privileges`. 이미지 볼륨 폴더는 Dockerfile에서 미리 `app` 소유로 만든다 | 기본(root) 실행 — 앱 취약점이 곧 컨테이너 root 권한 |
| 5 | BFF Origin 검사를 프록시 뒤에서 어떻게 유지하나 | 허용 출처를 **`NOOK_PUBLIC_ORIGIN`으로 명시**. 운영(`NODE_ENV=production`)에서 값이 없으면 전부 거절(fail-closed). 개발은 자기 주소로 대체 | 검사 끄기(CSRF 2차 방어 상실), `X-Forwarded-Host` 신뢰(헤더 위조 시 우회) |
| 6 | 언제 인터넷에 여나 (dep-plan 결정 6) | 5단계 SG 80·443은 **내 IP만**, 7단계 완료 뒤 `0.0.0.0/0`으로 전환 | 7단계를 5단계 앞으로 — 순서를 바꾸면 실제 프록시·도메인 없이 ForwardedHeaders 등을 검증해야 함 |

## 결과

- **좋아진 점**
  - 로컬 compose가 운영과 같은 `Production` 환경·같은 이미지로 돈다. 실측(2026-11-07): Origin 같은 출처 401·외부/없음 403, `/me` 직접 접근 404, 업로드 파일이 볼륨에 UID 1654로 저장·Caddy 경유 `200 image/jpeg`
  - 설정 누락이 조용히 열린 문이 되지 않는다(구현체 fail-fast, Origin fail-closed)
  - 운영 api 이미지에 SDK·dotnet-ef가 없다. 마이그레이션 실패는 api 교체 전에 드러난다(8단계 롤백 전제)
- **대가**
  - EF는 번들을 **만들 때도 실행할 때도** `Program.cs`를 `Build()` 직전까지 돌려 DbContext를 얻는다 → 번들 빌드에 자리표시 연결 문자열, migrate 컨테이너에 Provider 설정이 필요하다(`compose.yaml` `x-providers`). 끊으려면 `IDesignTimeDbContextFactory`가 필요하지만 개발 중 `dotnet ef` 사용법이 바뀌어 보류
  - `compose up`마다 migrate가 다시 돈다(적용할 것이 없으면 변경 없음)
  - `backend`가 `internal`이라 api가 인터넷에 못 나간다 → 7단계 SES·S3 호출 때 api만 egress 네트워크에 추가해야 한다
  - 통합 테스트 factory도 Provider 설정을 넣어야 한다(`NookApiFactory`)
- **남은 일**: ForwardedHeaders(신뢰 프록시 명시)·`UseHttpsRedirection` 정리·보안 헤더(7단계), 운영 compose는 `build:` 대신 ECR `image:`(8단계), arm64 빌드(8단계)
- **교훈**: 같은 포트 3300에 `npm run dev`(`0.0.0.0`·`[::]`)와 Docker(`127.0.0.1`)가 동시에 떠도 충돌 오류가 없고, 브라우저 `localhost`가 dev로 갔다. compose 검증은 리스너(`netstat`)와 도착지(DB 행)로 확인한다
