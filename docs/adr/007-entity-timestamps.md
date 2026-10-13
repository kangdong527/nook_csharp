# ADR-007 엔티티 시각: `DateTimeOffset` 인자 + `TimeGuard`

- 상태: 채택됨
- 날짜: 2026-10-12
- 결정자: 강동진
- 작성: Claude (사용자 요청, 결정 내용과 측정은 D2-4 세션)

## 맥락

D2-2에서 "리뷰 작성일은 Domain이 `TimeProvider`로 정한다"(R-RV-03)고 정했다. D2-4에서 첫 엔티티 `Bookmark`의 `createdAt`을 정하면서, 시각을 **서비스(`TimeProvider`)로 받을지 값(`DateTimeOffset`)으로 받을지**를 다시 따졌다.
엔티티가 늘면서 같은 시각 검증이 여러 곳에 반복될 것도 예상되었다.

## 결정

1. 엔티티는 시각을 **`DateTimeOffset` 인자**로 받는다. "지금"은 Application이 `TimeProvider.GetUtcNow()`로 **한 번** 정한다
   - 엔티티가 서비스에 기대지 않아 Domain이 순수하다
   - 테스트에서 가짜 `TimeProvider` 없이 값을 넣으면 된다
   - 한 유스케이스에서 여러 엔티티(예: `Group`과 첫 `GroupMember`)가 같은 시각을 가진다
   - `Review`도 같은 방식으로 통일한다. R-RV-03의 **UTC 날짜 계산은 Domain**에 남는다(`Review.ReviewDate`)
2. 검증은 **정적 가드 `TimeGuard.Utc()`**(`Nook.Domain.Common`)로 모은다
   - `default`(0001-01-01): 호출부가 시각을 빠뜨린 것 → 예외
   - 오프셋이 0이 아님: 정해진 경로(`GetUtcNow()`)를 벗어남, 대개 `DateTimeOffset.Now` 직접 호출 → **변환하지 않고 예외**
   - 검증한 값을 그대로 돌려줘서 `new X(..., TimeGuard.Utc(createdAt))` 한 줄로 끝난다
   - `[CallerArgumentExpression]`으로 예외의 `ParamName`이 원래 인자 이름(`createdAt`)이 된다(측정 확인)

## 고려한 대안

- **`TimeProvider`를 엔티티 메서드 인자로**: 테스트 고정은 가능하지만 엔티티가 서비스에 의존하고, 엔티티마다 `GetUtcNow()`를 따로 불러 같은 트랜잭션 안에서 시각이 달라진다 → 기각
- **값 객체 `UtcTimestamp`**: 시그니처가 검증을 강제한다. 그러나 시각이 들어오는 길은 엔티티마다 `private` 생성자 + `Create` 한 곳뿐이라 빠뜨려도 바로 보이고, D3에서 시각 컬럼마다 값 변환기가 필요하며 `GetUtcNow()` 결과를 매번 감싸야 한다 → 기각
- **비UTC를 `ToUniversalTime()`으로 변환해 수용**: 호출 실수가 조용히 덮인다 → 기각

## 결과

- Domain은 들어온 값이 "지금"인지 확인할 수 없다. 과거 시각을 넣어 R-RV-03을 우회하는 버그는 Application 테스트(D4)가 잡아야 한다
- 가드 호출을 빠뜨려도 컴파일된다 → D4에서 엔티티마다 `Create(..., default)`가 예외인지 검증한다
- Npgsql 6+는 오프셋이 0이 아닌 `DateTimeOffset`을 `timestamptz`에 쓰면 예외를 던진다. Domain의 UTC 강제는 이 제약과 같은 방향이며, 실패를 저장 전에 앞당긴다(D3에서 확인)
- 시각을 갖지 않는 엔티티도 있다(`Place`, `User`): 그 시각을 쓰는 규칙·화면이 없으면 넣지 않는다
