# 진행 내역

- 시작일: 2026-10-08
- 현재: **D1 진행 중** — 솔루션 골격 완료, 유스케이스·ADR-001 남음
- 다음 할 일: 원본 기능명세서 읽기 → `docs/design/usecases.md` 작성(유스케이스마다 실패 흐름 1개+) → ADR-001 → `feat/d1-skeleton` PR

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

커밋: `6b0d250` 골격 · `40e1f97` 패키지 보안 업데이트 · `a92da37` 프로젝트·참조 (모두 `feat/d1-skeleton`, PR 미생성)
Claude가 작성: `.gitattributes`, `dotnet new`/참조 명령어

## 체크리스트

### 1주차 — 설계와 정합성
- [ ] D1 도메인 분석·유스케이스·솔루션 골격·ADR-001
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
- 배포 대상(단일 VM vs Azure vs Fly.io) — D12 전까지
- API 스타일(Controllers vs Minimal API) — ADR-001 (골격은 Controllers로 생성함)
- 리포지토리 인터페이스 위치(Domain vs Application) — ADR-001
- Infrastructure→Domain 직접 참조 유지 여부, 전이 참조 차단(`DisableTransitiveProjectReferences`) — ADR-001
- 브랜치 접두사 `chore/*` 허용 시 CLAUDE.md 반영
- PR 머지 방식(merge / squash / rebase)

## 막힌 점
(없음)
