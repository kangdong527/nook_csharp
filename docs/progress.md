# 진행 내역

- 시작일: 2026-10-08
- 현재: **D1 완료** (2026-10-09) — `feat/d1-usecases` 푸시·PR 남음
- 다음 할 일: `feat/d1-usecases` 푸시·PR → **D2** 엔티티·값 객체 설계(R-STAR → `StarState`, R-GR → `Group.AddMember()`, 엔티티/값 객체 분류) — design-review 🔴 0

## 세션 기록

### 2026-10-08 (D1-1)
1. 템플릿 결정: .NET 10 + ASP.NET Core **Web API**(`webapi`) — `webapp`(Razor)은 프론트 미구현 방침과 불일치
2. 프론트 확장성 확인: API 분리 구조라 SPA·Razor·별도 Web 프로젝트 모두 추가 가능 (조건: DTO, ProblemDetails, CORS)
3. 폴더 구조 확정: 최상단에 솔루션, `src/<Project>`, `tests/<Project>`
4. `git init` → `.gitattributes` 추가(`* text=auto eol=lf`, 바이너리 제외) → `--renormalize`
5. 초기 커밋 → 브랜치 `master`→`main`, 메시지 Conventional Commits로 amend → GitHub 원격 연결·푸시
6. 언어 통계 왜곡(스킬 예제 .ts/.sh) → `.claude/** linguist-vendored` → PR #1로 머지 (main 직접 커밋 회피)
7. 머지 후 정리: 작업 브랜치에 `main` 반영, `git fetch --prune`으로 stale 원격 추적 브랜치 제거
8. VS로 만든 프로젝트(`Nook\Nook`, 이름·위치 불일치) 삭제 → CLI로 재생성: `dotnet new sln -n Nook`, `dotnet new webapi -n Nook.Api -o src/Nook.Api -f net10.0 --use-controllers --auth None`
9. NU1903(Microsoft.OpenApi 2.0.0, GHSA-v5pm-xwqc-g5wc) → `Microsoft.AspNetCore.OpenApi` 10.0.10→10.0.12, 골격과 별도 커밋
10. 계층 참조 설계: Api→Application·Infrastructure, Application→Domain·Algorithms, Infrastructure→Application·Domain, Domain·Algorithms는 참조 없음(leaf), 테스트→대상
11. 나머지 classlib 4개·xunit 3개 생성, 참조 연결 (Infrastructure 참조 누락 발견 후 수정)
12. 검증: `dotnet build` 오류 0, `dotnet test` 3/3 통과, 취약 패키지 0 → `feat/d1-skeleton` 커밋·푸시

커밋: `6b0d250` 골격 · `40e1f97` 패키지 보안 업데이트 · `a92da37` 프로젝트·참조 (모두 `feat/d1-skeleton`, PR #2로 머지)
Claude가 작성: `.gitattributes`, `dotnet new`/참조 명령어

### 2026-10-08 (D1-2)
1. 브랜치 `feat/d1-usecases` 생성, `.gitignore`에 `cs_trainning/`·`CLAUDE.local.md` 제외 추가
2. 유스케이스 작성 요령 학습: 시스템 경계, 기본/실패 흐름(갈라지는 단계 번호), 사후 조건은 화면이 아닌 DB 상태, "실패 시 보장"
3. 원본 확인: 장소 검색은 프론트가 카카오 SDK로 직접 수행(`src/lib/kakao.ts`) → 백엔드 유스케이스는 "선택한 장소 확정"(PL-01)부터. 원본 `upsertPlace`(`src/lib/store.ts:125`)는 조회→INSERT 분리로 TOCTOU
4. PL-01 장소 확정(Claude 예시), BM-01 장소 저장(사용자 작성, 리뷰 3회) — 별 상태는 북마크·리뷰 조합으로 계산되는 값, 존재하지 않는 장소, 동시 요청 실패 흐름 포함
5. BM-02 빈 틀 추가(`TODO(human)`)

커밋: `9f10d27` .gitignore · `6ea1d40` PL-01·BM-01 (`feat/d1-usecases`, 미푸시)

### 2026-10-09 (D1-3)
1. BM-02 저장 해제 — 별 상태 비대칭, 실패 시 보장은 "요청 전과 비교해 변하지 않는 것"
2. 공통 규칙 절 도입(R-PL·R-BM·R-RV·R-GR·R-STAR) — 규칙은 한 곳, 유스케이스는 ID 참조
3. R-BM-2 저장·해제 경합: 나중에 처리된 요청이 최종 상태(초안 "둘 다 실패"는 서버가 동시를 감지할 수 없어 폐기)
4. RV-01 리뷰 작성 — 싼 검증 → 이미지 업로드 → DB 저장 순서, 고아 이미지는 "노출되지 않음 + 정리 배치"
5. GR-01 그룹 합류 — 미리 검사(3b·4b)와 최종 보장(5a·5b) 구분, 불변식 3줄(정원 ≤ 4, 참여 ≤ 3, 멤버십 0/1)
6. 나머지 유스케이스 17개 목록(이름·실패 흐름 키워드), 원본 명세에서 "리뷰 같은 장소 1일 1회" 발견
7. ADR-001 채택: Controllers, 리포지토리 인터페이스 Application, 전이 참조 차단 + Infrastructure→Domain 유지, 엔티티 겸용 + Fluent API + 지연 로딩 미사용
8. `Directory.Build.props`에 `DisableTransitiveProjectReferences` — Api에서 Domain 타입 사용 시 CS0234 확인
9. 검증: `dotnet build` 경고·오류 0, `dotnet test` 3/3 통과

커밋: `411fc6e` BM-02·공통 규칙 · `97f7926` RV-01 · `b6cda93` GR-01 · `b39b259` build props · `d06fdea` 유스케이스 목록 · `c455bec` ADR-001 · `190f71c` 학습 로그 (`feat/d1-usecases`, 미푸시)
Claude가 작성: PL-01 예시, 공통 규칙 재구성, R-BM-1·2, GR-01 불변식, 유스케이스 목록 17개, ADR-001 빈칸·결과 절, `Directory.Build.props`
Claude가 작성: PL-01(예시), BM-01·BM-02 빈 틀

## 체크리스트

### 1주차 — 설계와 정합성
- [x] D1 도메인 분석·유스케이스·솔루션 골격·ADR-001
- [ ] D2 클래스·엔티티·값 객체 (design-review 🔴 0)
- [ ] D3 ERD·스키마·EF Core 매핑·마이그레이션·ADR-002
- [ ] D4 테스트 설계(단위 + Testcontainers 통합)
- [ ] D5 핵심 API(장소·북마크·리뷰·좋아요·JWT)
- [ ] D6 트랜잭션·동시성(그룹 합류) — incident 001
- [ ] D7 DSA #1 근처 장소 검색(Geohash) — dsa/01

### 2주차 — 대용량·성능·배포
- [ ] D8 대용량 시드·인덱스·Keyset — incident 002, 003
- [ ] D9 쿼리 튜닝·N+1·카운터 전략
- [ ] D10 k6 부하 테스트·커넥션 풀 — incident 004
- [ ] D11 LRU 캐시·Token Bucket — dsa/02, 03
- [ ] D12 Docker·CI/CD·HTTPS 배포·ADR-003
- [ ] D13 운영 검증·장애 시나리오
- [ ] D14 README·회고

## 미결 결정
- 초대 코드 제외 문자 확정(R-GR-05 초안: I·O·0·1 제외 32자, SSoT와 일치하는지) — D2
- 같은 회원이 같은 장소에 리뷰를 몇 개 쓸 수 있나(중복 발행 처리 포함) — D2. 원본 명세: "같은 장소 1일 1회, 00시 기준 초기화"(장소 상세 바텀시트 2-5). 채택 여부와 기준 시간대(KST/UTC) 결정 필요
- 리뷰 이미지 저장 형식: URL vs 스토리지 키 — D3
- 고아 이미지 판별 기준(미참조 + 생성 후 N시간)과 정리 배치 — D3
- 별 상태: 저장 컬럼 vs 조회 시 계산 — D3
- 북마크 테이블에 "북마크 여부" 컬럼이 필요한가(행 존재 = 북마크) — D3
- 중복 저장/미저장 해제 요청을 오류로 볼지 성공(멱등)으로 볼지 — D5
- 북마크 API: 토글 vs PUT/DELETE — D5
- 배포 대상(단일 VM vs Azure vs Fly.io) — D12 전까지
- 브랜치 접두사 `chore/*` 허용 시 CLAUDE.md 반영
- PR 머지 방식(merge / squash / rebase)

## 막힌 점
(없음)
