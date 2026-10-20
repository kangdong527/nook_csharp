# 학습 로그

> 매일 세션 끝에 **직접** 쓴다. Claude는 형식만 맞춘다.

## 템플릿

### D1-1 (2026-10-08) — 프로젝트 구조 설계
- 배운 것:
  1. 프로젝트(서버단) 생성: ASP.NET Core API
  2. 프로젝트 구조(트리) 및 컴포넌트 참조 관계 이해 및 설계: Api, classlib, xunit
  3. 깃허브 연동 및 브랜치-커밋-푸시-머지-풀 흐름
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께):
- 내일 확인할 것:

---

### D1-2 (2026-10-08) — 설계 문서 작성
- 배운 것:
  1. 유스케이스 피드백 동반하며 4개 항목 작성(BM-01, BM-02, RV-01, GR-01)
  2. ADR 작성: 주요 설계 결정(도메인 참조 방식, 도메인<->엔티티 분리 여부) 및 트레이드오프 분석
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): PL-01 유스케이스 — 첫 유스케이스(SC-01) 작성이 어려워 예시로 요청함. 사용자 초안 SC-01은 주석으로 남겨 비교. 공통 규칙 절(R-PL-1·2, R-STAR 표)로 재구성 — 사용자 요청, 내용은 사용자가 쓴 BM-01·02 규칙. R-BM-1·2와 1c(저장·해제 경합) — 사용자 요청, 사용자 초안은 "둘 다 실패". GR-01 사후 조건의 불변식 3줄 — 사용자 요청. 나머지 유스케이스 목록(17개, 이름·실패 흐름 키워드) — 사용자 요청. ADR-001의 빈칸(2·4번 대안·이유, 3번 정리 메모, 결과 절) — 사용자 요청
- 내일 확인할 것:

---

### D2-1 (2026-10-10) — 실패 표현 기반과 첫 값 객체
- 배운 것:
  1. 결과 반환 정의: Error, Result 클래스 작성(feat: 제네릭, 성공 시 null 리턴 고민)
  2. 리뷰 값 객체(ReviewContent) 클래스 작성: 객체 생성 및 유효성 검사, Result & Error 활용한 반환 구현
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `ReviewErrors.cs` 전체와 `ReviewContent`의 오류 반환 줄 — Result에 오류를 반환하는 코드에서 막혀 요청(힌트 4단계). `ReviewContent` 리뷰 후 결함 수정(struct→sealed record, null 입력 처리, `Value` 공개, 길이 1회 계산, 상수 PascalCase) — 사용자 요청
- 내일 확인할 것:

---

### D2-2 (2026-10-10) — 값 객체 3개와 분류표
- 배운 것:
  1. 좌표 값 객체(Coordinate), PlaceErrors 클래스 작성 (feat: 도메인에서는 물리적(지구) 범위만 검증, )
  2. 닉네임 값 객체(Nickname), UserErrors 클래스 작성 (feat: 검증 - 제한 글자, 코드포인트 기준 길이, 한글 완성형 등)
  3. 초대 값 객체(InviteCode), GroupsErrors 클래스 작성 (feat: 코드 생성 로직, 허용 형태 정의 등)
  4. 도메인 모델 작성: 작성하며 나온 ID 없는 규칙에 대한 규칙 유스케이스에 추가 및 따라오는 내용 수정
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `Coordinate`·`Nickname`·`InviteCode` 빈 틀(시그니처까지, 본문은 사용자). `.editorconfig`와 BOM·CRLF·마지막 개행 일괄 정리 — 사용자 요청. `domain.md` 분류표 틀. `usecases.md` 규칙 R-US-01·02, R-LK-01, R-GR-06·07 문구와 R-BM 참조 번호 변경 — 사용자 요청, 내용은 사용자 결정. R-RV-03 초안(사용자가 반영). 분류표 🟡 수정(R-US-02→User, R-GR-06 추가, Like 표기, R-GR-07 근거 보강) — 사용자 요청
- 내일 확인할 것:

---

### D2-3 (2026-10-11) — 애그리거트 경계와 클래스 다이어그램
- 배운 것:
  1. 도메인 모델 - 클래스 다이어그램 작성
  2. 다이어그램 작성 결과 분석 후, 새 규칙 추가(그룹의 최소 사용자 수) 및 분류 표 수정(그룹 행 - 사용자의 그룹 탈퇴 시, 현재 그룹 내 인원을 세기 전에 그룹 행 잠금: 동시 탈퇴에 대한 처리)
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): 다이어그램 빈 블록·`TODO(human)` 틀. `usecases.md` R-GR-08·09 문구와 R-GR-09 동시성 근거 — 사용자 요청, 내용은 사용자 결정. `progress.md` 갱신
- 내일 확인할 것:

---

### D2-4 (2026-10-12) — 엔티티와 강타입 ID
> 이 칸은 사용자 요청으로 Claude가 세션 기록을 바탕으로 작성했다.

- 배운 것:
  1. 엔티티 기준을 Bookmark 하나에서 정하고 나머지가 따르게 했다: 동등성 재정의 안 함(비교하는 코드가 없고 중복은 DB PK가 막음), 실패할 규칙이 없으면 `Result` 없이 반환, 시각은 `DateTimeOffset` 인자 + `TimeGuard`. 같은 `Create`라도 **입력이 어디서 오느냐**(서버 코드 → 예외, 회원 → `Result`)가 실패 표현을 정한다
  2. 강타입 ID는 `sealed record` + `IEntityId<TSelf>`. 측정해 보니 비제네릭 공통 기반만 `userId == placeId`가 컴파일되고, 제네릭 기반·인터페이스는 CS0019로 막혔다. 인터페이스의 목적은 중복 제거가 아니라 D3 제네릭 값 변환기(`T.From`)
  3. 정합성 장치는 범위가 다르다: 엔티티 메서드(인스턴스 1개) < EF 식별 맵(DbContext 1개 = 요청 1개) < DB 제약·잠금(모든 요청). `Group`의 `.Any()` 같은 루트 검사는 미리 검사일 뿐 최종 보장이 아니다. 형식 검증(외부가 정한 형식은 느슨하게)과 크기 검증(불신 입력이면 항상)도 다른 축이다
- 막혔던 것 / 어떻게 풀었나:
  - 식별 맵이 R-BM-01을 지킨다고 생각함 → 요청마다 DbContext가 따로라 동시 요청은 DB PK만 막는다는 걸 확인(CS 퀴즈 F3과 같은 오해)
  - `internal`의 범위, `sortOrder`를 누가 지키는지 모름 → "B를 지우면 C는?" 같은 구체 장면으로 풀어 보니 컬렉션 규칙은 루트, 순서는 목록 인덱스로 매기면 된다는 결론
  - 컴파일러가 잡지 않는 사고 4건(`Reviews/UserId.cs` 파일명, 복사한 주석, 자동 `using`, `ReviewErrors` namespace 삭제) → 커밋 전 `git diff`·`grep`으로 발견
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `PlaceId`·`ReviewImageId`(같은 패턴 반복, 사용자 요청). `KakaoPlaceId` 64자 상한 검증 코드(사용자 요청, 상한값은 사용자 결정). `Review.cs` 키 검사 순서 주석(사용자 요청, 결정은 사용자). 엔티티 빈 틀·`TODO(human)`. `domain.md`·`usecases.md`의 스토리지 키·R-RV-03 문구 반영. `progress.md` 갱신. **이 학습 로그 칸과 ADR-004~007**(사용자 요청, 결정 내용은 사용자)
- 내일 확인할 것:
  - `Group` 질문 3개(그룹명 R-GR-10, `RemoveMember` 반환, `AddMember` 인자·검사 순서)
  - ADR-004~007을 읽고 사용자 결정과 다른 서술이 있으면 고치기

---

### D2-5 (2026-10-13) — Group TDD와 D2 마무리
- 배운 것:
  1. GroupName, GroupMember 작성
     - 그룹명 규칙 R-GR-10: 한글 완성형·영문·숫자 + ASCII 공백(가운데·연속 공백 허용), 2~6자는 공백 포함. 이모지는 일단 제외 — 허용 문자는 나중에 넓히는 게 싸다(기존 데이터가 그대로 유효). 그룹명은 유일하지 않음
     - 허용 문자 검사를 `Nickname`과 공유하지 않고 복사: R-US-01과 R-GR-10은 다른 이유로 바뀌는 규칙(공백 허용에서 이미 갈림)
     - `GroupMember`는 `internal(같은 컴포넌트 내 호출만)` 내부 엔티티. 그룹 안 식별자는 `UserId`(자연 키)라 별도 Id·부모 `GroupId` 없음, 탈퇴는 수정이 아니라 컬렉션 제거라 setter 없음
  2. Group의 Create, AddMember, RemoveMember를 TDD로 구현
     - `Create`는 `Result` 없이 `Group` 반환(그룹명은 이미 검증된 값 객체라 회원 입력 실패가 없음). 만든 회원이 첫 멤버(R-GR-08), 방장(owner)은 두지 않음 — 읽는 유스케이스가 없음
     - `AddMember(UserId, joinedAt)` → `Result`: 중복(`AlreadyMember`)을 정원(`GroupFull`)보다 먼저 검사(회원이 바꿀 수 없는 구체적 사실 먼저), 둘 다 `Conflict`. 초대 코드는 인자로 받지 않음(Application이 그룹을 찾는 데만 씀)
     - `RemoveMember`는 멱등, 반환은 `MemberRemoval { NotMember, Removed, GroupEmptied }`. `NotMember`를 기본값 0으로 둬 미초기화 값이 그룹을 지우지 않게 함. 빈 그룹에 합류시키면 호출 코드 실수로 보고 예외. enum은 `Groups/` 바로 아래
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `GroupTests` 빈 틀·`TODO(human)`. ADR-004 결정 5(`RemoveMember` 구현)·6(방장 없음) 추가와 ADR-006 `GroupId` 반영(사용자 요청, 결정은 사용자). design-review 🔴 수정 — `Group.Create` null 인자 테스트 2개, null 검사, `Group.cs` 헤더 주석(사용자 요청). `progress.md` 갱신. "배운 것" 1·2의 하위 항목(사용자 요청, 내용은 사용자 결정)
- 내일 확인할 것:
  - PR 머지 후 D3 브랜치 생성
  - design-review 🟡 3건(`progress.md` 미결 결정) 고칠지

---

### D3-1 (2026-10-15) — ERD 초안(테이블 8개)
- 배운 것:
  1. ERD 작성 요령: 
    - 쿼리, 컬럼별 타입 지정, 제약(PK, FK, UNIQUE, CHECK)
    - 작성된 유스케이스, 도메인, 클래스 소스코드를 참고하며 일관성 유지 노력
  2. 잠금 이해 및 충돌표 읽기: FOR KEY SHARE(FK가 있는 INSERT는 부모 행에 걸림), FOR NO KEY UPDATE(키 변경 없는 요청임을 약속), FOR UPDATE(키 변경 포함일 수 있는 요청)
  3. ON DELETE(RESTRICT vs CASECADE) 결정 근거 도출
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `erd.md` 골격(형식만 — 시간 부족, 사용자 요청). places `ck_places_kakao_place_id` 행·`ck_places_name` 근거, users 메모 2줄(`btrim` 범위, `lower()` 일치 미확인), §3 `lower()`·북마크 여부 행, review_images "확인 항목", groups "FK (들어오는 것)" 행 — 모두 사용자 요청, 결정 내용은 사용자. `progress.md` 갱신
- 내일 확인할 것:
  - ERD §3 남은 미결 4개 → §4 원본 비교 → ADR-002

---

### D3-2 (2026-10-16) — ERD §3 미결 결정 마무리
- 배운 것:
  1. 고아 이미지 정리(feat: prefix[장소 vs **✔️시간 - 데이터가 10년 치로 쌓여도 매일 훑는 양은 하루치로 거의 일정**], 최근은 매일 + 1주일 한번 전체 스캔 방식 도입, 업로드 완료 후부터 커밋 전 배치 수행에 대한 이미지 제거 문제 해결을 위한 나이 조건(N) 도입)
  2. TDD 방식 - ReviewImage 스토리지 키 검증
  3. 별 상태 저장 여부: 저장 안함 결정 -> 상태 계산 비용 미미 예상, 저장 시 lost update(북마크, 리뷰 시 리뷰 락 및 북마크 대기 후, 후 커밋으로 인한 gray) 위험
  4. 자식 컬렉션 타입: 해당 객체 컬렉션 의미(이미지 정렬, 그룹 멤버 순서)에 따라 지정, 근거 세움
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): erd.md §3.1 정리(사용자 내용 이전 + 리뷰 🔴·🟡 반영), review_images 컬럼·UNIQUE·Q4·메모 갱신, §3 고아 이미지 행 선택/이유 분리와 N·범위 반영, C안 도입 조건 문구, §3 별 상태 행 선택·이유, `ReviewImage.cs`·`Review.cs` 주석 정리 — 모두 사용자 요청(문서 여러 곳에 퍼져 있어 직접 수정 시 실수 우려), 결정 내용은 사용자. Claude가 결정(사용자 위임): 키 길이 검사 단위 `Length`. `progress.md` 갱신
- 내일 확인할 것:
  - ERD §4 원본 `schema.sql` 비교 → ADR-002
  - Mermaid 다이어그램(ERD §1) → EF Core 매핑·마이그레이션

---

### D3-3 (2026-10-17) — ERD 다이어그램과 EF Core 매핑·마이그레이션
- 배운 것:
  1. ERD mermaid 다이어그램 작성(feat: 식별과 비식별 구분, DB 제약 내용(앱 X)만 라벨로 작성)
  2. Insfrastructure -> Configuration 및 강타입 EntityIdConverter, DbContext 및 OnModelCreate 작성(feat: Configuration 등록 방식 -> 현재 규모에 따라 Assembly 방식 대신 하나씩 등록)
  3. 도메인 > 복원용 메서드(From) 작성: 규칙을 다시 검사하지 않고 빈 값 여부만 => "DB에 들어간 값은 CHECK를 통과했다" 신뢰
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): erd.md §3 "그룹 멤버 최소 1명 DB 보장" 행(기각 근거 중 '숨은 로직·늦은 실패'·'얻는 것이 작다'는 Claude가 덧붙임 — 확인 필요), `compose.yaml`·`.env.example`(보일러플레이트), EF Core 패키지·`dotnet-ef` 로컬 도구·user-secrets 설정, `Nickname`·`Email` `From` 테스트와 주석, `KakaoPlaceId`·`ReviewContent`·`GroupName`·`InviteCode` `From`과 테스트, `EntityIdConverter`·`NookDbContext`·Configuration 6개(users·places·bookmarks·reviews+review_images·likes·groups+group_members), `Place` EF 전용 생성자, `Init` 마이그레이션 SQL 3개, `AddInfrastructure` DI 등록, 라운드트립 실험 설계·실행, ADR-008, `progress.md` 갱신 — 사용자 요청(작성 위임), 결정은 사용자: 마이그레이션 위치(Infrastructure)·시작 프로젝트(Api), 버전 맞춤(명시적 참조), `From`은 빈 값만 검사, 이름 명시, 마이그레이션 A안(users 먼저 → Init으로 합침), Place 생성자 A안, DEFERRABLE은 EF 모델 밖, `ImmutableArray` 확인 닫기, 그룹 최소 1명은 DB 미보장
- 내일 확인할 것:

---

### D4-1 (2026-10-18) — Testcontainers 통합 테스트 기반
- 배운 것:
  1. 테스트 간 공유 컨테이너 작성 및 용도 학습: 같은 컬렉션 `[Collection(컬렉션명 ex: postgres)]`으로 설정한 IntegrationTestBase 클래스를 상속받은 테스트(`GroupPersist, ReviewPersist`)는 같은 컨테이너를 공유하고 순차로 실행되기 때문에, 한 테스트의 데이터 Reset이 다른 테스트가 실행되는 도중에 끼어들지 않는다는 의미이다
  2. DB 컨텍스트 코드 작성 시: 저장(그룹 추가) -> 조회(추가된 그룹) 순서면 저장 시 사용한 컨텍스트가 아닌 검증용 컨텍스트를 새로 만들어서 식별 맵이 DB를 거치도록 한다
  3. 실험 및 SQL 로그를 통한 EF가 UPDATE 순서를 정하는 기준 실험: id(Guid) 가 작은 행부터 수행, Guid 는 **"같은 밀리초에 만든 UUIDv7은 대소가 무작위로 생성"**되기 때문에 실행 시 마다 순서가 바뀜 
  4. sortOrder 맞바꾸기에서 순서를 바꿔도 DEFERRED가 필요한 이유 => **3** 의 Guid 무작위 생성으로 인한 순서가 고정되지 않기 때문에, sortOrder 맞바꾸기 시 트랜잭션 내 중복값을 잠시 허용을 위해 사용
- 막혔던 것 / 어떻게 풀었나:
- TDD 기록: `Create_WhenSaved_ReloadsCreatorAsOnlyMember` — 첫 실행 초록 → `db.Users.Add(creator)` 제거로 빨간불 확인(`23503 fk_group_members_users`) → 원복 후 초록
  - `SaveChanges_WhenSortOrdersSwapped_ReloadsInNewOrder` — 첫 실행 초록 → 같은 트랜잭션에서 `SET CONSTRAINTS ... IMMEDIATE`로 빨간불(`23505 ux_review_images_review_sort_order`) → 원복 후 초록
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `PostgresFixture`·`IntegrationTestBase`(보일러플레이트), Testcontainers.PostgreSql 4.16.0·Infrastructure/Domain 참조 추가, Integration `UnitTest1.cs` 삭제, `GroupPersistenceTests` 첫 테스트(사용자 요청 — 나머지 통합 테스트의 예시), `ReviewPersistenceTests` 맞바꾸기 테스트·`TestResult.Unwrap`(사용자 요청), `progress.md` 갱신, `PostgresFixture.CreateContext(sqlLog)` SQL 로깅 옵션(사용자 요청)·맞바꾸기 테스트 주석을 실측에 맞게 수정, `BookmarkTests` 시각 가드 테스트(사용자 요청 — ADR-007 나머지 4개 엔티티의 예시), `GroupTests`의 `Create`·`AddMember` 시각 가드 테스트(사용자 요청, 빨간불 확인은 사용자), `LikeTests`·`ReviewTests` 시각 가드 3개(사용자 요청 — D4부터 테스트 코드는 대부분 Claude가 작성, 사용자는 예측·결과 확인), `erd.md` 확인 항목·실측 표기 갱신, Configuration·마이그레이션 인덱스 Q 번호 주석(사용자 요청, A안은 사용자 결정), `Init.cs` DEFERRABLE 주석을 실측으로, namespace 분석기 설정(IDE0130·CA1050 error 결정은 사용자), D4 게이트 누락분 테스트(`ReviewContentTests` Create 경계, `ReviewTests` 3장, `CheckConstraintTests` — 범위 A안은 사용자 결정), D4 미결 결정 프로브·테스트·구현(`ReviewContent` NFC·앞뒤 서식 문자 제거, `Group.AddMember` `joinedAt < CreatedAt` 예외 — 결정은 사용자가 추천안 채택)
- 내일 확인할 것:

---

### D5-1 (2026-10-18) — 인증 방식 결정, JWT 검증 골격, magic_link_tokens
- 배운 것:
  1. 
  2. 
  3. 
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `JwtOptions`·`AccessTokenIssuer`(`BuildClaims` 제외)·`Program.cs` 인증 설정(JwtBearer·Fallback 정책·`IncludeErrorDetails`), JwtBearer 패키지 추가, 서명 키 생성·user-secrets 설정(`openssl` 실패 후 대체), 보안 조사(OWASP·RFC 8725), erd.md `magic_link_tokens` 설계 전체, usecases.md AU-01~03 수정, `MagicLinkToken`·`MagicLinkTokenConfiguration`·`AddMagicLinkTokens` 마이그레이션(식 인덱스 SQL)·로컬 DB 적용, `CheckConstraintTests` CHECK 3개, `progress.md` 갱신 — 모두 사용자 요청. 결정은 사용자: 매직 링크, API 자체 발급, Access만(60분), 지킬 것(`sub` = 사용자 ID, 발급 한 곳), Fallback·`IncludeErrorDetails` 적용, 별도 테이블(근거: 가입자는 users 행이 없다), AU-01 응답 통일, 정책값(15분·3회·1일), 개발 토큰 엔드포인트 기각. 사용자 작성: `BuildClaims`
- 내일 확인할 것:
  - AU-01·AU-02 Application 유스케이스 + `/auth/magic-link`·`/auth/magic-link/consume` (`TODO(human)`)
  - 질문 미답: Q1을 `SELECT` 후 `UPDATE` 두 문장으로 나누면 무슨 일이 생기나? / `BuildClaims`의 `TryParse`가 실제로 막는 경우는?

---

### D5-2 (2026-10-20) — 동시 소모·TryParse 정리, AU-01 계층 설계와 SharedKernel
- 배운 것:
  1. 
  2. 
  3. 
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `Nook.SharedKernel` 분리(Result·Error 이동, using·참조 수정 — 기계적 리팩터링), AU-01 뼈대 전체(Application 계약 2개·`MagicLinkPolicy`·`MagicLinkSecret`·핸들러 껍데기, Infrastructure 저장소·Channel 큐·워커·콘솔 발송기·DI, Api `AuthController`·등록·설정) — 협업 영역의 뼈대, 핸들러 본문은 `TODO(human)`. 결정은 사용자: 백그라운드 B안, 형식 오류 400, SharedKernel A안, sub 형식 위반 = 500
- 내일 확인할 것:
  - `RequestMagicLink.HandleAsync` `TODO(human)` 구현 → 테스트 → 손 테스트 → 커밋
  - 질문 미답: `mail.Enqueue`를 INSERT 앞에 두면? / Q2(세기)와 INSERT 사이 동시 요청은?

---

### D5-3 (2026-10-20) — AU-01 핸들러, 매직 링크 TOCTOU와 메일 폭탄 방어, 일정 변경
- 배운 것:
  1. 
  2. 
  3. 
- 막혔던 것 / 어떻게 풀었나:
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `HandleAsync`의 Q2 속도 제한·리팩터링(사용자 요청 — 본문 초안·INSERT→Enqueue 순서는 사용자), `Nook.Application.Tests`·핸들러 테스트 7개(D4부터 테스트는 Claude 작성, 사용자는 예측), 일정 변경 문서 반영. 결정은 사용자: 일정 변경(D6 → FE → DEP 1주, EC2·GitHub Actions), TOCTOU 방어 ② A3 + Q2 + A1
- 내일 확인할 것:
  - AU-02 consume
  - 예측이 빗나간 것: "Fake 시계와 실제 시각이 어긋난다" — 왜 안 어긋났는지 한 줄로

---
