# ADR-006 강타입 ID: `sealed record` + `IEntityId<TSelf>`

- 상태: 채택됨
- 날짜: 2026-10-12
- 결정자: 강동진
- 작성: Claude (사용자 요청, 결정 내용과 측정은 D2-4 세션)

## 맥락

첫 엔티티 `Bookmark.Create(userId, placeId, ...)`를 쓰면서 ID 표현을 정해야 했다. ID가 모두 `Guid`라면 `Create(placeId, userId, ...)`처럼 순서를 바꿔 넣어도 컴파일된다(원본 nook은 모든 ID가 `string`이라 같은 위험이 있었다).
ID 타입이 셋째(`ReviewId`)가 되는 시점에, 같은 코드를 복사할지 공통으로 묶을지도 정해야 했다.

## 결정

1. ID마다 **`sealed record class`**로 `Guid`를 감싼다(`UserId`, `PlaceId`, `ReviewId`, `ReviewImageId`, `GroupId`)
   - `record struct`가 아닌 이유: `default`나 배열 원소가 `Guid.Empty`를 가진 **멀쩡해 보이는 ID**가 된다. class면 `null`이 되어 nullable 경고로 드러난다
2. 생성 경로는 둘이다
   - `New()`: 새 ID 발급. `Guid.CreateVersion7()`(시간순이라 PK B-tree 삽입이 인덱스 끝에 모인다 — **미측정**, D8에서 v4와 비교)
   - `From(Guid)`: DB 값 복원. `Guid.Empty`는 정상 흐름에서 나올 수 없으므로 예외
3. 공통화는 **인터페이스 + 타입별 최소 복사**(C안)
   ```csharp
   public interface IEntityId<TSelf> where TSelf : IEntityId<TSelf>
   {
       Guid Value { get; }
       static abstract TSelf From(Guid value);
   }
   ```
   - 인터페이스의 목적은 중복 제거가 아니라 **제네릭 코드에서 생성하기**다. D3에서 `ValueConverter<T, Guid> where T : IEntityId<T>` 하나로 모든 ID를 `T.From(guid)`로 복원한다

## 측정 (scratchpad, 2026-10-12)

| 방식 | `userId == placeId` | `userId.Equals(placeId)` |
|---|---|---|
| A. 비제네릭 공통 기반 record | **컴파일됨**, 항상 False | 컴파일됨 |
| B. 제네릭 자기 타입 기반 record | CS0019 | 컴파일됨 |
| C. 인터페이스 + 복사 | CS0019 | 컴파일됨 |

`Equals(object)` 교차 비교는 어느 방식도 막지 못한다.

## 고려한 대안

- **A. 비제네릭 공통 기반**: 서로 다른 ID 비교가 컴파일되어 조용히 False → 기각
- **B. 제네릭 기반 record**: `Guid.Empty` 검사를 기반 생성자 한 곳에 둘 수 있다. 그러나 record 상속(`EqualityContract` 등)이 붙고, D3 변환기를 위해 결국 인터페이스도 필요해 구조가 두 겹이 된다 → 기각
- **소스 생성기(StronglyTypedId, Vogen)**: 복사가 사라지지만 Domain이 외부 패키지에 의존해 leaf 원칙이 깨진다 → 기각

## 결과

- ID를 추가할 때마다 `Value`, `private` 생성자, `New`, `From`을 복사한다. 실제로 D2-4에서 복사 실수 3건(파일 이름 `UserId.cs`, "회원" 주석 잔존, 메시지 조사 누락)이 나왔다 → 컴파일러가 잡지 못하므로 D4에서 리플렉션으로 모든 `IEntityId<>` 구현의 `From(Guid.Empty)` 예외를 한 테스트로 검증한다
- 클라이언트가 경로로 `Guid.Empty`를 보내면 라우팅 제약(`{id:guid}`)을 통과해 `From`이 예외 → 500. Api/Application에서 미리 거를지 D5에서 정한다
- v7 ID에는 생성 시각이 들어 있어 노출 시 가입·작성 시점이 드러난다. 이 값을 데이터로 쓰지 않는다
