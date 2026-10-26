# 003. 그룹 동시 합류에서 정원이 넘쳤고, FOR UPDATE를 걸어도 그대로 넘쳤다

- 날짜 / 관련 커밋: 2026-10-25 / `339d328`
- 분류: 동시성
- 작성: Claude (사용자 요청)

## 1. 증상

- 멤버 1명(빈자리 3)인 그룹에 5명이 동시에 합류하면 **5명 모두 성공**하고 그룹이 **6명**이 됐다. 정원은 4명(R-GR-01)이다.
- 원본 nook의 알려진 결함 "정원 초과"(TOCTOU)와 같은 증상이다.
- 첫 해결 시도(그룹 행 `FOR UPDATE`)를 넣어도 **결과가 바뀌지 않았다**(6명). 잠금이 실행되는데도 막지 못했다.
- 사용자 영향: 정원을 넘긴 그룹이 생긴다. DB에는 이를 막는 제약이 없어(여러 행에 걸친 규칙이라 CHECK 불가) 조용히 저장된다.

## 2. 재현

| 항목 | 값 |
|---|---|
| 환경 | 로컬 Windows, Testcontainers `postgres:17`, READ COMMITTED(기본) |
| 데이터 | 만든 회원 1명인 그룹 1개, 합류할 회원 5명 |
| 동시성 | 요청마다 새 DI 스코프(= DbContext·연결 하나). `TaskCompletionSource` 출발선에 5개를 세운 뒤 한 번에 출발 |
| 테스트 | `JoinGroupTests.Join_ConcurrentIntoGroupWithThreeSeats_KeepsFourMembers` |
| 명령 | `dotnet test tests/Nook.Integration.Tests --filter "FullyQualifiedName~Join_Concurrent" --logger "console;verbosity=detailed"` |
| 재현율 | 잠금 없음 **5/5**, 실험 1(잠금 + JOIN 한 문장) **1/1** |

테스트는 `성공 n · 실패 n · DB 멤버 n명`을 출력하고, 최종 4명 · 성공 3 · 나머지는 전부 `GroupFull`을 단언한다.

## 3. 가설과 검증

| # | 가설 | 검증 방법 | 결과 |
|---|---|---|---|
| 1 | 5개 요청이 모두 커밋 전 스냅샷(멤버 1명)을 읽어 `AddMember`가 각자 통과한다 | 잠금 없는 핸들러(찾기 → `AddMember` → `SaveChanges`)로 테스트 실행 | **맞음.** 성공 5 · 6명 (5/5) |
| 2 | 트랜잭션만 열면 막힌다 | 추론(실행 안 함): READ COMMITTED의 일반 SELECT는 잠그지 않고, 서로 다른 `(group_id, user_id)` INSERT는 PK에 걸리지 않는다 | 기각. 공통 행을 잠그지 않으면 대기가 생기지 않는다 |
| 3 | 그룹 행을 `FOR UPDATE`로 잠그면 한 명씩 줄을 서서 막힌다 | `FromSql("SELECT * FROM groups WHERE invite_code = … FOR UPDATE")` + 핸들러 트랜잭션 | **막히지 않음.** 성공 5 · 6명 (1/1) |
| 3-a | EF가 `FOR UPDATE`를 어딘가에서 잃어 잠금 자체가 없다 | 탐침 테스트로 실제 SQL 로깅(`Db.CreateContext(output.WriteLine)`) | 기각. `FOR UPDATE`는 서브쿼리 안에 살아 있다 |
| 3-b | 잠금은 걸리지만, 멤버를 같은 문장의 JOIN으로 읽어 **대기 전 스냅샷**을 본다 | 같은 SQL 로그에서 문장 모양 확인 → 잠금과 멤버 로딩을 두 문장으로 나눠 재실행 | **맞음.** 나눈 뒤 성공 3 · 실패 2 · 4명 (5/5) |

가설 3-a와 3-b는 결과(6명)가 같다. 결과만 보고 "잠금이 효과 없다"고 결론 내렸다면 오진했을 것이다. SQL 로그가 둘을 갈랐다.

EF가 실제로 보낸 SQL(실험 1):

```sql
SELECT n0.id, n0.created_at, n0.invite_code, n0.name, g.group_id, g.user_id, g.joined_at
FROM (
    SELECT n.id, n.created_at, n.invite_code, n.name
    FROM (
        SELECT * FROM groups WHERE invite_code = @p0 FOR UPDATE
    ) AS n
    LIMIT 2
) AS n0
LEFT JOIN group_members AS g ON n0.id = g.group_id
ORDER BY n0.id, g.group_id
```

`Members`는 owned 컬렉션(`OwnsMany`)이라 EF가 `FromSql`을 서브쿼리로 감싸고 `group_members`를 **같은 문장에서** JOIN한다.

## 4. 근본 원인

**1차 (잠금 없음): Check-then-Act.**
`Group.AddMember`의 정원 검사는 메모리의 `_members.Count`를 본다. 요청마다 Group 인스턴스(스냅샷 복사본)가 따로 있으니, 객체 하나 안의 캡슐화는 다른 요청의 복사본을 막지 못한다. DB에도 "4명 이하"를 표현할 제약이 없다. `ConfirmPlace`는 UNIQUE + `ON CONFLICT`가 최종 방어선이었지만, 합류에는 그런 방어선이 없다.

**2차 (실험 1): 잠금 대기 뒤 최신으로 다시 보는 것은 잠긴 행뿐이다.**
READ COMMITTED에서 한 문장의 스냅샷은 **문장이 시작될 때** 잡힌다. `FOR UPDATE`가 다른 트랜잭션의 잠금을 기다렸다가 풀리면, PostgreSQL은 **잠근 groups 행만** 최신 버전으로 다시 확인한다(EvalPlanQual). 같은 문장 안에서 JOIN한 `group_members` 행은 대기 전 스냅샷 그대로다. 그래서 줄은 섰지만, 줄 선 모든 요청이 "멤버 1명"을 읽었다.

```
시간 │ 요청 A                              │ 요청 B (실험 1: 한 문장)
─────┼─────────────────────────────────────┼──────────────────────────────────────
 t1  │ BEGIN                               │ BEGIN
 t2  │ SELECT … FOR UPDATE → 🔒 획득         │
 t3  │                                     │ SELECT … FOR UPDATE ⏳ (스냅샷은 지금 잡힘)
 t4  │ 멤버 1명 → AddMember OK → INSERT     │ ⏳
 t5  │ COMMIT → 🔓                          │ 🔒 획득. groups 행만 최신으로 다시 확인
 t6  │                                     │ JOIN한 멤버는 t3 스냅샷 → 1명 ❌
 t7  │                                     │ AddMember OK → INSERT → COMMIT (정원 초과)
```

**왜 이전엔 드러나지 않았나.**
코드만 보면 "잠그고 → 읽는다"로 보인다. `FromSql` 한 줄이 owned 컬렉션 로딩 때문에 JOIN 한 문장으로 바뀌는 것은 SQL 로그를 봐야만 알 수 있다. 단일 요청 테스트(흐름 3개)는 모두 통과했다.

## 5. 해결

**잠그는 문장과 읽는 문장을 나눈다** — `GroupRepository.LockByInviteCodeAsync` (`src/Nook.Infrastructure/Groups/GroupRepository.cs`)

```
LockByInviteCodeAsync(inviteCode)
│
├─ 문장 1 ── SELECT id AS "Value" FROM groups
│            WHERE invite_code = 'ABCDEF' FOR UPDATE
│   │  다른 트랜잭션이 이 행을 잠그고 있으면 → COMMIT/ROLLBACK까지 대기
│   └─ locked : List<Guid>
│
├─ locked is []          → return null                     (없는 코드 → 404)
├─ locked is not [var id] → throw InvalidOperationException (2개 이상 = UNIQUE 위반, 데이터 손상)
│
└─ 문장 2 ── db.Groups.SingleAsync(g => g.Id == id)
             새 문장 = 새 스냅샷 → 잠금을 얻기 전에 커밋된 멤버가 전부 보인다
             SingleAsync(OrDefault 아님): 행을 잠그고 있으니 그 사이 삭제될 수 없다
```

- `ToListAsync`인 이유: `SingleOrDefaultAsync`는 SQL을 서브쿼리로 감싼다(`PlaceRepository.TryAddAsync`와 같은 이유).
- `is not [var id]`는 "원소가 정확히 1개"인 리스트 패턴이다. `Count != 1` 검사와 `locked[0]` 꺼내기를 한 줄로 합친 것이다.

**트랜잭션 경계는 핸들러가 쥔다** — `JoinGroupHandler` (`src/Nook.Application/Groups/JoinGroup.cs`, D5-3 결정)

```
HandleAsync(command)
│
├─ ① InviteCode.Create                       ← 트랜잭션 밖. DB가 필요 없는 일은 잠그기 전에 끝낸다
│     └─ 형식 오류 → return InviteCodeInvalidFormat (400)
├─ ② userId = UserId.From(...)
│
├─ ③ BEGIN ─────────────── await using var tx = uow.BeginAsync()
│   ┣─ ④ LockByInviteCodeAsync              🔒 여기서부터 잠금 유지
│   ┃     └─ null → return GroupNotFound (404)        └ Dispose = ROLLBACK, 잠금 해제
│   ┣─ ⑤ group.AddMember(userId, now)       ← 최신 멤버로 메모리에서 판단
│   ┃     ├─ 이미 멤버 → return AlreadyMember (409)    └ ROLLBACK
│   ┃     └─ 4명 이상 → return GroupFull (409)        └ ROLLBACK
│   ┣─ ⑥ SaveAsync → DetectChanges → INSERT group_members
│   ┗─ ⑦ COMMIT                              🔓 잠금 해제
└─ ⑧ return Success (200)
```

- 실패 경로에 롤백 코드가 없다. `await using`이 커밋하지 않은 트랜잭션을 `Dispose`하면 롤백되고, 잠금도 그때 풀린다.
- 정원 판단은 `Group.AddMember` 한 곳에 남는다. 잠금은 "최신 멤버를 보고 판단하게" 해 줄 뿐이고, 규칙 자체는 도메인에 있다.

해결 후 타임라인(5명이면 이 줄이 다섯 번 이어지고, 4·5번째는 t8에서 4명을 읽어 `GroupFull`):

```
시간 │ 요청 A                              │ 요청 B
─────┼─────────────────────────────────────┼──────────────────────────────────────
 t1  │ BEGIN                               │ BEGIN
 t2  │ 문장1 FOR UPDATE → 🔒 획득            │
 t3  │                                     │ 문장1 FOR UPDATE → ⏳
 t4  │ 문장2 → 멤버 1명                     │ ⏳
 t5  │ AddMember OK (1 → 2)                │ ⏳
 t6  │ INSERT group_members                │ ⏳
 t7  │ COMMIT → 🔓                          │ 🔒 획득, id 반환
 t8  │                                     │ 문장2 (새 스냅샷) → 멤버 2명 ✅
 t9  │                                     │ AddMember OK (2 → 3) → INSERT → COMMIT
```

**검토했지만 버린 대안** (상세는 ADR-010)

| 대안 | 버린 이유 |
|---|---|
| 트랜잭션만 열기 | 공통 행을 잠그지 않아 대기가 생기지 않는다(가설 2) |
| 조건부 INSERT (`… WHERE (SELECT count(*) …) < 4`) | 서로 다른 행을 INSERT하니 경합할 한 행이 없다. COUNT 서브쿼리는 각자 스냅샷을 읽는다 |
| `groups.member_count` 카운터 + 조건부 UPDATE | 작동하고 `CHECK (member_count <= 4)`로 DB 이중화까지 되지만, 멤버 수가 바뀌는 경로 5곳(생성·합류·탈퇴·그룹 삭제·회원 탈퇴)에서 동기화가 필요하다. 근거로 든 "목록에 멤버 수 표시"는 아직 유스케이스가 없다 |
| SERIALIZABLE + 재시도 | 재시도가 없으면 성공 1 · 40001 실패 4 · 2명이 된다(빈자리가 있는데 거절). 재시도 루프가 필요하고, 같은 인덱스 페이지의 다른 그룹과도 가짜 충돌이 날 수 있다 |
| `count(*) … FOR UPDATE` 한 문장 | PostgreSQL이 집계 함수와 `FOR UPDATE`를 함께 허용하지 않는다 |

## 6. 결과 (Before → After)

| 단계 | 성공 | 실패(`GroupFull`) | DB 멤버 | 반복 |
|---|---|---|---|---|
| Before: 잠금 없음 | 5 | 0 | **6명** | 5/5 |
| 실험 1: `FOR UPDATE` + JOIN 한 문장 | 5 | 0 | **6명** | 1/1 |
| **After: 잠금 문장 → 읽기 문장 분리** | **3** | **2** | **4명** | **5/5** |
| 변이: After에서 `FOR UPDATE`만 제거 | 5 | 0 | 6명 (빨간불) | 3/3 |

- 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 221개 통과(Domain 95 · Algorithms 1 · Application 7 · Integration 118).
- 측정하지 않은 것: 잠금 대기로 늘어나는 지연 시간. 그룹당 최대 4명이라 대기열이 짧을 것으로 예상하지만 숫자는 없다 → D10 k6에서 같은 그룹 동시 합류 p95로 확인.
- 실험 1의 재현율은 1회뿐이다. 원인은 SQL 로그로 확인했다(3-b).

## 7. 재발 방지

- 테스트 `Join_ConcurrentIntoGroupWithThreeSeats_KeepsFourMembers`: 최종 인원, 성공 수, 실패 종류(`GroupFull`)까지 단언한다. `FOR UPDATE`를 지우는 변이를 3/3 잡는다.
- `GroupRepository` 주석에 "잠금과 멤버 로딩을 한 문장으로 합치면 안 된다"와 그 이유(실험 1 수치)를 남겼다.
- `IGroupRepository` 주석: "트랜잭션 안에서 불러야 의미가 있다(밖이면 문장 끝에 바로 풀린다)".
- 같은 패턴을 쓸 다음 자리: 탈퇴 Write Skew(R-GR-09), 회원당 그룹 수(R-GR-04, users 행). **"잠그는 문장"과 "세는/읽는 문장"을 나눈다**를 리뷰 체크리스트로 쓴다.

## 8. 배운 것

- **잠갔다 ≠ 최신 값을 읽었다.** READ COMMITTED에서 잠금 대기 뒤 최신으로 다시 보는 것은 잠근 행뿐이다. 같은 문장 안의 다른 테이블은 대기 전 스냅샷이다.
- ORM 한 줄이 어떤 SQL이 되는지는 로그를 봐야 안다. owned 컬렉션은 `FromSql`을 서브쿼리 + JOIN으로 바꾼다.
- 결과가 예측과 맞아도 원인은 따로 확인한다. "잠금이 빠졌다"와 "스냅샷이 옛것이다"는 같은 6명을 낸다.
