# ADR-008 EF Core 매핑 경계: 복원 메서드 · EF 전용 생성자 · 모델 밖 제약

- 상태: 채택됨
- 날짜: 2026-10-17
- 결정자: 강동진
- 작성: Claude (사용자 요청, 결정 내용과 측정은 D3-3 세션)

## 맥락

D3-3에서 Domain(ADR-001: 참조 없음, leaf)을 EF Core로 매핑하면서, Domain 설계와 EF의 동작 방식이 네 곳에서 부딪혔다.

1. **값 객체 복원**: `Nickname.Create`처럼 값 객체는 `Result<T>`를 반환하고, `Result`에는 `Value`가 없어 `TryGetValue(out, out)`로만 꺼낸다(D2). 그런데 EF 값 변환기는 식 트리라서 `out` 변수를 쓸 수 없고, 실패를 표현할 수도 없다
2. **강타입 ID 복원**: `IEntityId<TSelf>.From`(정적 추상, ADR-006)을 변환기 람다에서 직접 부르면 CS8927(식 트리에 정적 추상 인터페이스 멤버를 넣을 수 없음)이 난다
3. **복합 프로퍼티와 생성자**: EF는 복합 프로퍼티(`Place.Coordinate`)를 생성자 매개변수로 넘기지 못한다 — `No suitable constructor was found for the type 'Place'`
4. **EF 모델이 표현하지 못하는 스키마**: `lower()` 표현식 UNIQUE(`HasIndex`는 프로퍼티 목록만 받음), `DEFERRABLE` UNIQUE(EF 마이그레이션 API에 없음). 반대로 EF는 설계하지 않은 인덱스를 만든다(FK 컬럼마다 자동 인덱스)

## 결정

1. **값 객체는 생성(`Create`)과 복원(`From`)을 나눈다**
   - `Create(string?)`: 사용자 입력 → 정규화·전체 규칙 검사 → `Result<T>`
   - `From(string)`: DB 값 → **빈 값만** 검사(`ArgumentException.ThrowIfNullOrWhiteSpace`, 위반은 데이터 손상으로 보고 예외) → 값을 **바꾸지 않고** 그대로 담는다
   - 형식 규칙은 다시 검사하지 않는다. **저장된 값은 DB CHECK가 보장**한다(새 값의 기준은 Domain, 저장된 값의 기준은 CHECK). 그래서 Domain 규칙이 바뀌어도 기존 행을 읽을 수 있다
   - 값을 바꾸지 않는 이유: `Trim`·정규화를 하면 EF 스냅샷이 DB와 다른 값으로 시작한다
   - 대상: `Nickname`·`Email`·`KakaoPlaceId`·`ReviewContent`·`GroupName`·`InviteCode`. 강타입 ID의 `From`(`Guid.Empty`만 막음)과 같은 기준
2. **강타입 ID는 제네릭 변환기 하나로** — `EntityIdConverter<TId> where TId : IEntityId<TId>`를 `ConfigureConventions`에서 타입마다 한 번 등록한다. CS8927은 `TId.From`을 일반 정적 메서드(`FromGuid`)로 한 번 감싸 피한다
3. **`Place`에 EF 복원 전용 private 생성자를 둔다** — `Coordinate`를 뺀 매개변수로 만들고, 직후 EF가 `Coordinate`를 채운다. 앱 코드는 쓰지 않으며 이유를 주석으로 남긴다
4. **스키마의 기준은 erd.md이고, EF 모델은 표현할 수 있는 만큼만 담는다**
   - EF가 표현하지 못하는 제약 3개(`ux_users_nickname_lower`·`ux_users_email_lower`·`ux_review_images_review_sort_order`)는 `Init` 마이그레이션의 `migrationBuilder.Sql`로 만든다
   - DEFERRABLE UNIQUE는 EF 모델에 **알리지 않는다**. 알리면 순서 맞바꾸기(0↔1)의 UPDATE 순서를 EF가 순환으로 볼 수 있고, 커밋 시점 검사는 DB가 맡는다
   - EF의 FK 자동 인덱스(`ForeignKeyIndexConvention`)를 끈다. 인덱스는 쿼리에서 출발해 erd.md에 적은 것만 둔다(예: `bookmarks.place_id`는 보조 인덱스 없음)
   - 이름은 모두 erd.md 그대로 명시한다(`ToTable`·`HasColumnName`·`HasName`·`HasDatabaseName`·`HasConstraintName`). 이름 규칙 패키지는 erd.md 접두사(`ux_`·`ck_`)와 맞지 않는 부분을 결국 명시해야 한다

## 고려한 대안

- **복원도 `Create`를 거치고 실패하면 예외**: 규칙이 바뀌면(예: 닉네임 하한 2→3자) 기존 회원과 그 회원이 걸린 리뷰·그룹 조회가 모두 예외가 된다. 식 트리 제약 때문에 감싸는 코드도 따로 필요하다 → 기각
- **`Create`에 예외 로직을 넣기**: 사용자 입력 오류(예상 가능한 실패)까지 예외가 되어 D2의 "예상 가능한 실패는 Result" 원칙이 깨진다 → 기각
- **`Place`의 매개변수 없는 private 생성자**: 모든 프로퍼티가 잠깐 비는 상태가 되어 범위가 넓다 → 기각
- **`Place`가 위도·경도를 따로 갖고 `Coordinate`를 계산 프로퍼티로**: 값 객체 설계(D2)를 되돌리고 접근마다 할당한다 → 기각
- **`Coordinate`를 `OwnsOne`으로**: owned 타입도 생성자 바인딩이 안 돼 해결되지 않는다 → 기각
- **DEFERRABLE UNIQUE를 EF 모델에도 `IsUnique()`로 알림**: 위 순환 위험 → 기각
- **`EFCore.NamingConventions`**: 위 접두사 불일치 → 기각

## 결과

- **규칙을 바꿀 때는 Domain 상수와 CHECK를 함께 바꾸는 마이그레이션**이 필요하다. 기존 데이터를 유예할지 백필할지는 그 마이그레이션에서 정한다
- 앱을 거치지 않은 쓰기(SQL 직접 수정·시드)는 CHECK만 통과하면 `From`으로 읽힌다. CHECK가 빠진 컬럼이 생기면 이 전제가 깨진다
- `From`은 `public`이라 Application이 사용자 입력에 잘못 쓸 수 있다 → 호출 제한(`internal` + `InternalsVisibleTo` vs ArchUnitNET)은 D5에 결정(progress 미결)
- SQL로 만든 제약 3개는 **EF 모델 스냅샷에 없다**. 바꾸거나 지울 때도 SQL로 해야 하고, `dotnet ef migrations add`가 그 변경을 감지하지 못한다
- 같은 이유로 **테스트 스키마는 `EnsureCreated`가 아니라 마이그레이션으로 만든다**(D4 Testcontainers). `EnsureCreated`는 모델만 보므로 세 제약이 빠진 채 테스트가 통과한다
- FK 자동 인덱스를 껐으므로 **새 FK를 추가할 때 인덱스가 필요한지 직접 판단**해야 한다(부모 삭제 시 FK 검사·CASCADE가 자식 테이블을 훑는지)
- `Place` 생성 직후 `Coordinate`가 잠깐 `null`인 구간이 있다. EF 내부에서만 생기고 앱 코드에는 드러나지 않는다

## 측정 (D3-3, PostgreSQL 17.10 · compose)

- 다른 `DbContext`로 다시 읽기: 새 인스턴스(`ReferenceEquals` false), 값 일치 → 생성자 바인딩·`From` 복원 동작. 같은 컨텍스트로 다시 읽으면 같은 인스턴스(식별 맵)
- `Coordinate`(복합 프로퍼티)·`Images` 순서(`SortOrder == i`)·`Members`(OwnsMany)·`timestamptz` offset 0 복원 확인
- DEFERRABLE: 트랜잭션 안 0↔1 맞바꾸기는 COMMIT 성공, 커밋 시점에도 남은 중복은 COMMIT에서 위반
- `EXPLAIN`(`enable_seqscan = off`, 빈 테이블 — 속도가 아니라 인덱스 사용 가능 여부): `lower(nickname) = lower(?)`는 `ux_users_nickname_lower` Index Scan, `nickname = ?`는 Seq Scan. `WHERE review_id = ?`는 `ux_review_images_review_sort_order` Bitmap Index Scan
