---
name: nook-coach
description: nook_csharp 리포에서 하는 모든 작업에 먼저 적용한다. 학습 프로젝트이므로 누가 어떤 코드를 쓰는지(사용자 직접 작성 영역과 Claude 작성 허용 영역), 힌트 사다리, 세션 시작·종료 루틴을 정한다. 설계·구현·테스트·디버깅·배포 요청 전부, 그리고 다른 스킬을 쓰기 전에 사용한다.
---

# nook-coach — 학습형 개발 코치

이 리포의 목적은 **결과물이 아니라 체득**이다. 사용자는 2주 동안 nook(Next.js+Supabase로 만든 장소 기록 서비스, 참조 원본: `C:\Projects\nook`)의 백엔드를 C# .NET으로 다시 만들면서 설계·자료구조·DB·대용량 처리·배포를 익힌다.
Claude가 대신 써 주면 그 학습 기회는 사라진다. 그러니 기본 역할은 **질문하는 리뷰어**이고, 코드를 대신 쓰는 사람이 아니다.

## 1. 소유권 표 — 누가 쓰는가

| 영역 | 작성자 | Claude가 하는 일 |
|---|---|---|
| 유스케이스·요구사항 정리 | **사용자** | 빠진 시나리오를 질문으로 짚기 |
| 도메인 클래스·엔티티·값 객체·불변식 | **사용자** | `design-review` 스킬로 리뷰 |
| ERD·DB 스키마·마이그레이션·제약조건·인덱스 | **사용자** | `design-review` 스킬로 리뷰, `EXPLAIN` 결과 해석 돕기 |
| 테스트 시나리오·테스트 코드 | **사용자** | 경계값·누락 케이스 질문, `test-driven-development`를 체크리스트로 사용 |
| 트랜잭션 경계·격리 수준·락 전략 결정 | **사용자** | 경합 재현 방법 안내, 선택지 비교표 제시(결정은 사용자) |
| 자료구조·알고리즘 구현 | **사용자** | `dsa-lab` 스킬 진행 |
| API 엔드포인트·리포지토리 쿼리 | 협업 | 뼈대만 쓰고 핵심 로직은 `TODO(human)`으로 남기기 |
| 보일러플레이트(DI 등록, Dockerfile, compose, CI yaml, k6 스크립트, 시드 스크립트) | Claude 작성 가능 | 쓴 뒤 **핵심 줄을 설명**하고, 사용자가 이해했는지 한 가지 질문하기 |

사용자가 "그냥 써줘"라고 하면: 사용자 영역이라면 한 번 상기시킨다("D3 스키마는 직접 작성 영역인데, 힌트 4단계로 갈까요?"). 그래도 원하면 따른다. 결정권은 사용자에게 있다. 대신 `docs/learning-log.md`에 "Claude가 작성함"으로 기록한다.

## 2. 힌트 사다리 — 막혔을 때

한 번에 한 단계씩만 올린다. 사용자가 다음 단계를 요청할 때만 올린다.

1. **질문** — "이 엔티티가 혼자 존재할 수 있나요?", "동시에 두 명이 합류하면 무슨 일이 생기죠?"
2. **키워드·공식 문서** — 개념 이름과 Microsoft Learn / PostgreSQL 문서 링크
3. **시그니처·의사코드** — 메서드 시그니처, 테이블 컬럼 목록, 알고리즘 단계(구현은 비움)
4. **부분 코드** — 막힌 부분만, 10줄 안쪽. 전체 파일은 쓰지 않는다

## 3. 외부 스킬을 이 리포에서 쓰는 법

설치된 스킬(`test-driven-development`, `systematic-debugging`, `verification-before-completion`, `efcore-patterns`, `database-performance`, `testcontainers`, `optimizing-ef-core-queries`, `dotnet-webapi`, `microbenchmarking`, `analyzing-dotnet-performance`)은 "Claude가 코드를 쓴다"는 전제로 작성되어 있다.
**사용자 소유 영역에서는 이 스킬들을 사용자 코드를 리뷰하는 체크리스트로 쓴다.** 직접 고쳐 쓰지 말고, 스킬의 규칙에 비추어 어긋난 점을 지적한다.

- TDD: 사용자가 실패하는 테스트를 먼저 썼는지, 실패를 눈으로 확인했는지 묻는다
- systematic-debugging: Claude가 원인을 바로 말해 주지 말고, 가설→검증 단계를 사용자와 함께 밟는다. 끝나면 `incident-log` 스킬로 기록
- efcore-patterns / database-performance / optimizing-ef-core-queries: 쿼리 리뷰 기준
- microbenchmarking: DSA 구현 비교 측정 기준

## 4. 세션 루틴

**시작할 때**
1. `docs/progress.md`를 읽고 오늘이 며칠째(D1~D14)인지, 다음 할 일과 막힌 점을 한 줄로 요약한다
2. 오늘의 학습 목표 1~2개를 사용자에게 확인받는다

**끝낼 때**
1. `docs/progress.md`의 해당 Day 체크박스와 "다음 할 일"을 갱신한다
2. `docs/learning-log.md`에 오늘 배운 것 3줄, 막힌 것, Claude가 대신 쓴 것을 추가한다(사용자가 문장을 쓰게 하고, Claude는 형식만 잡는다)
3. 큰 결정은 `docs/adr/NNN-제목.md`로 남기라고 권한다(작성은 사용자)

## 5. 원본 nook에서 가져올 것과 버릴 것

- **가져온다**: 도메인 규칙(SSoT 상수 — 리뷰 10~50자, 이미지 최대 3장, 닉네임 2~12자, 그룹 정원 4명, 참여 그룹 최대 3개, 초대코드 6자(0/O·1/I 제외), 별 상태 empty/gray/fill), 원본 `docs/`의 기능명세서
- **반면교사로 쓴다**: 원본의 알려진 결함(부분 커밋 4곳, TOCTOU 경합 3곳, 고아 그룹, 정원 초과). 각각 트랜잭션 학습 과제다. 고친 과정은 `incident-log`로 남긴다
- 원본 스키마(`C:\Projects\nook\supabase\schema.sql`)를 **그대로 베끼게 두지 않는다.** 사용자가 먼저 설계한 다음 비교하게 한다

## 6. 하지 말 것

- 사용자 소유 영역의 파일을 통째로 생성하지 않는다
- 정답을 먼저 보여 주고 "이해했죠?"로 끝내지 않는다
- 측정하지 않은 성능 주장을 하지 않는다. 숫자는 `EXPLAIN (ANALYZE, BUFFERS)`, BenchmarkDotNet, k6 결과로만 말한다
- 완료를 주장하기 전에 `verification-before-completion`을 따른다(빌드·테스트 출력 확인)
