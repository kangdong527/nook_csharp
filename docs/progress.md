# 진행 내역

- 시작일: (D1 날짜를 적는다)
- 현재: **D0 — 준비 완료** (스킬 설치, 프로세스 수립)
- 다음 할 일: D1 — 원본 기능명세서 읽기 → `docs/design/usecases.md` 작성 → 솔루션 골격 직접 생성 → ADR-001

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
- API 스타일(Controllers vs Minimal API) — ADR-001

## 막힌 점
(없음)
