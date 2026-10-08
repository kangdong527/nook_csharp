---
name: dsa-lab
description: 자료구조·알고리즘 학습을 nook의 실제 문제와 연결해 진행한다. 문제 정의 → 복잡도 분석 → 후보 비교 → 사용자가 C#으로 직접 구현(TDD) → BenchmarkDotNet 측정 → 서비스 적용 순서를 따른다. "알고리즘", "자료구조", "근처 장소 검색", "Top-K", "캐시 구현", "/dsa-lab" 요청에 사용한다.
---

# dsa-lab — 문제에서 출발하는 자료구조 학습

목표: "이 문제에는 왜 이 자료구조인가"를 설명할 수 있고, **라이브러리 없이 C#으로 직접 구현**할 수 있게 되는 것.
구현은 사용자가 한다. Claude는 `nook-coach`의 힌트 사다리를 따른다.

## 실험 한 번의 진행 순서 (각 단계 산출물은 `docs/dsa/NN-주제.md`에 사용자가 쓴다)

1. **문제 정의** — nook의 어떤 기능인가, 입력 크기(N)는 얼마인가, 읽기와 쓰기 비율은 어떤가
2. **브루트포스** — 가장 단순한 방법과 시간·공간 복잡도. 사용자가 먼저 구현한다. 이것이 비교 기준선이다
3. **후보 비교** — 2~3개 후보를 표로 비교한다(시간/공간/구현 난이도/업데이트 비용). 표는 사용자가 채우고 Claude가 검토한다
4. **구현 (TDD)** — `src/Nook.Algorithms/`에 구현하고 `tests/Nook.Algorithms.Tests/`에 테스트한다. 브루트포스 결과와 같은지 확인하는 **속성 기반 비교 테스트**(랜덤 입력 1000회)를 꼭 넣는다
5. **측정** — `benchmarks/Nook.Benchmarks/`에서 BenchmarkDotNet으로 N=1e3/1e5/1e6을 비교한다(`microbenchmarking` 스킬을 따른다). 이론 복잡도와 실측이 다르면 이유를 쓴다(캐시 지역성, 할당, GC)
6. **적용 또는 기각** — 서비스에 넣을지, DB(인덱스/PostGIS)나 Redis에 맡길지 결정하고 근거를 남긴다. 기각도 좋은 결론이다

## nook 문제 ↔ 자료구조 카탈로그

| # | nook 문제 | 브루트포스 | 학습 후보 | 비교할 실무 대안 |
|---|---|---|---|---|
| 1 | 지도 화면(bbox) 안의 장소 / 내 위치 근처 K개 | 전체 스캔 O(N) | **Geohash**(비트 인터리빙, 문자열 접두사), **QuadTree**, KD-Tree | PostgreSQL B-tree 복합 인덱스, PostGIS GiST |
| 2 | 장소 이름 자동완성 | `StartsWith` 스캔 | **Trie**(+ 노드별 Top-K 캐시) | `pg_trgm` GIN 인덱스 |
| 3 | 인기 리뷰/장소 Top-K | 전체 정렬 O(N log N) | **Min-Heap** O(N log K), Count-Min Sketch | Redis Sorted Set |
| 4 | 장소 상세 캐시 | 매번 DB 조회 | **LRU Cache**(Dictionary + 이중 연결 리스트, O(1)) | `IMemoryCache`, Redis |
| 5 | API Rate Limiting | 요청 로그 전체 저장 | **Token Bucket**, Sliding Window Log/Counter | ASP.NET Core `RateLimiter`, Redis |
| 6 | 초대코드 생성·중복 방지 | DB 조회 후 재시도 | 진법 변환(Base32 변형, 0/O·1/I 제외), 충돌 확률(생일 문제), **Bloom Filter** | `UNIQUE` 제약 + 재시도 |
| 7 | 피드 페이지네이션 | OFFSET | **B+Tree** 원리 이해 → Keyset(seek) 페이지네이션 | 복합 인덱스 `(created_at, id)` |
| 8 | 친구/그룹 간 취향 유사도 | 전 쌍 비교 O(N²) | 집합 연산, **Jaccard + MinHash** | — (확장 과제) |

2주 일정에서는 **#1(필수), #4 또는 #3 중 하나, #5**를 우선한다. 나머지는 여유가 있을 때 한다.

## 리뷰 기준

- 테스트가 경계 조건을 다루는가: 빈 입력, N=1, 같은 좌표 중복, 날짜변경선·극점(geohash), 용량 0/1(LRU), 버스트(token bucket)
- 불필요한 할당은 없는가: LINQ를 핫 루프에서 쓰는지, boxing, `struct`로 바꿀 후보
- 스레드 안전성: 서비스에 넣을 구조라면 동시 접근을 어떻게 처리하는가(lock, `ConcurrentDictionary`, 불변 스냅샷)
- 측정 결과에 Mean과 Allocated가 함께 있는가

## 학습 자료

- 공식 문서: Microsoft Learn — Collections, `Span<T>`, BenchmarkDotNet docs
- 원리를 확인할 때는 CLRS나 해당 자료구조의 원 논문/위키를 개념 확인용으로만 본다. 구현을 보고 베끼지 않는다
