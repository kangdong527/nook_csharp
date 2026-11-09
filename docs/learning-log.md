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
- 배운 것 (Claude가 세션 기록으로 정리 — 사용자 요청, 2026-10-20):
  1. 내가 결정한 것: 매직 링크 + API가 직접 발급하는 Access 토큰(HS256, 60분, Refresh 없음). 매직 링크는 users가 아닌 **별도 테이블** — 가입자는 링크를 요청할 때 닉네임이 없어 users 행을 만들 수 없기 때문. 메일 요청은 가입 여부·속도 제한과 무관하게 항상 202, 개발 전용 토큰 발급 엔드포인트는 운영 노출 사고 패턴이라 기각
  2. 질문으로 얻은 것: "상태 없는 JWT만으로 링크를 처리하면?" → 가능하지만 1회 사용·무효화·속도 제한을 잃고, 메모리 캐시로 대신하면 재시작·다중 서버에서 깨진다(상태를 없앤 게 아니라 옮긴 것). 마이그레이션 파일 3개의 역할: `.cs`(변경분, 손으로 고침) / `.Designer.cs`(그 시점 모델 사진) / `ModelSnapshot.cs`(최신 사진 — `migrations add`가 비교하는 대상은 DB가 아니라 이것). 손으로 넣은 SQL(식 인덱스)은 어느 사진에도 없다
  3. 핵심 개념: 인가는 **기본 잠금**(Fallback 정책 `RequireAuthenticatedUser`, 공개만 `[AllowAnonymous]`) — 새 엔드포인트가 실수로 열리지 않는다. 토큰은 DB에 **해시만** 저장(256비트 난수라 salt·느린 해시 불필요 — 판단 기준은 입력의 엔트로피). `sub`는 이메일·닉네임이 아닌 사용자 ID — 바뀌는 값을 식별자로 쓰지 않는다
- 막혔던 것 / 어떻게 풀었나: Api는 Domain을 못 봐서(ADR-001 결정 3) 발급기에 `UserId`를 썼다가 빌드 실패 → `Guid`로. `openssl`이 PowerShell에 없어 서명 키 생성 실패 → `RandomNumberGenerator`로 대체. 실행 중인 Api가 DLL을 잠가 마이그레이션 빌드 실패 → 서버 종료 후 진행
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `JwtOptions`·`AccessTokenIssuer`(`BuildClaims` 제외)·`Program.cs` 인증 설정(JwtBearer·Fallback 정책·`IncludeErrorDetails`), JwtBearer 패키지 추가, 서명 키 생성·user-secrets 설정(`openssl` 실패 후 대체), 보안 조사(OWASP·RFC 8725), erd.md `magic_link_tokens` 설계 전체, usecases.md AU-01~03 수정, `MagicLinkToken`·`MagicLinkTokenConfiguration`·`AddMagicLinkTokens` 마이그레이션(식 인덱스 SQL)·로컬 DB 적용, `CheckConstraintTests` CHECK 3개, `progress.md` 갱신 — 모두 사용자 요청. 결정은 사용자: 매직 링크, API 자체 발급, Access만(60분), 지킬 것(`sub` = 사용자 ID, 발급 한 곳), Fallback·`IncludeErrorDetails` 적용, 별도 테이블(근거: 가입자는 users 행이 없다), AU-01 응답 통일, 정책값(15분·3회·1일), 개발 토큰 엔드포인트 기각. 사용자 작성: `BuildClaims`
- 내일 확인할 것:
  - AU-01·AU-02 Application 유스케이스 + `/auth/magic-link`·`/auth/magic-link/consume` (`TODO(human)`)
  - 질문 미답: Q1을 `SELECT` 후 `UPDATE` 두 문장으로 나누면 무슨 일이 생기나? / `BuildClaims`의 `TryParse`가 실제로 막는 경우는?

---

### D5-2 (2026-10-20) — 동시 소모·TryParse 정리, AU-01 계층 설계와 SharedKernel
- 배운 것 (Claude가 세션 기록으로 정리 — 사용자 요청, 2026-10-20):
  1. 내가 결정한 것: 메일은 백그라운드 발송 B안(`Channel` + `BackgroundService`, 재시작 시 유실 수용). 이메일 형식 오류만 400 — DB를 보지 않고 판단하니 가입 여부가 드러나지 않는다. `Result`·`Error`는 `Nook.SharedKernel`로 분리(A안). 서명은 통과했는데 `sub`가 Guid가 아니면 401이 아니라 **500 + 에러 로그** — 서명된 클레임의 형식 위반은 사용자 잘못이 아니라 서버 버그
  2. 질문으로 얻은 것: 소모(Q1)를 `SELECT` 후 `UPDATE` 두 문장으로 나누면 동시 2요청이 둘 다 미소모를 읽어 토큰이 2번 쓰인다(TOCTOU). 한 문장 조건부 UPDATE는 행 잠금을 기다린 뒤 **최신 버전으로 WHERE를 다시 검사**해 0행 → 앱은 0행을 "무효 링크"로 처리. 위조 `sub`는 엔드포인트까지 오지 못한다(서명 검증 실패 → 인가 Fallback이 401). SharedKernel ≠ AOP(AOP는 실행 시점에 동작을 끼워 넣기, SharedKernel은 타입의 위치·참조 구조). Infrastructure는 별도 프로세스가 아니라 Api 프로세스에 로드되는 DLL — 그래서 같은 메모리의 Channel이 동작하고, 재시작하면 유실된다
  3. 핵심 개념: **계약은 쓰는 쪽(Application)에 둔다**(DIP) — 핸들러는 `IMagicLinkTokenStore`·`IMagicLinkMailQueue`만 알고, 구현은 Infrastructure. 핸들러가 구현 클래스를 받으면 의존 방향이 뒤집힌다. 같은 싱글턴을 두 이름(인터페이스·구현)으로 꺼내야 핸들러와 워커가 같은 큐를 본다
- 막혔던 것 / 어떻게 풀었나: AU-01 계약 위치를 처음에 `List<IMagicLinkToken>`(데이터)·`IEmailSender`를 Api에 두는 것으로 답함 → 힌트 3단계(시그니처) 후 "핸들러가 구현 클래스를 받으면 DIP가 깨진다"를 스스로 설명(CS F1 약점과 같은 패턴). 빌드 실패 `CS0012`(Api가 Domain의 `Result`를 못 봄) → ADR-001이 예상한 누출 → SharedKernel 분리
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `Nook.SharedKernel` 분리(Result·Error 이동, using·참조 수정 — 기계적 리팩터링), AU-01 뼈대 전체(Application 계약 2개·`MagicLinkPolicy`·`MagicLinkSecret`·핸들러 껍데기, Infrastructure 저장소·Channel 큐·워커·콘솔 발송기·DI, Api `AuthController`·등록·설정) — 협업 영역의 뼈대, 핸들러 본문은 `TODO(human)`. 결정은 사용자: 백그라운드 B안, 형식 오류 400, SharedKernel A안, sub 형식 위반 = 500
- 내일 확인할 것:
  - `RequestMagicLink.HandleAsync` `TODO(human)` 구현 → 테스트 → 손 테스트 → 커밋
  - 질문 미답: `mail.Enqueue`를 INSERT 앞에 두면? / Q2(세기)와 INSERT 사이 동시 요청은?

---

### D5-3 (2026-10-20) — AU-01 핸들러, 매직 링크 TOCTOU와 메일 폭탄 방어, 일정 변경
- 배운 것 (Claude가 세션 기록으로 정리 — 사용자 요청, 2026-10-20):
  1. 내가 결정한 것: 요청 순서 = 형식 검사 → Q2 → 토큰 생성 → **INSERT → Enqueue**(반대면 저장 전에 메일이 도착해 링크가 실패할 수 있다). TOCTOU는 허용하되 방어는 **② A3(IP 입구 제한) + Q2(이메일 DB 한도) + A1(워커 수신자별 상한)** — 막을 대상은 피해자 받은편지함과 발송 도메인 평판 둘 다. A1은 단일 소비자 전제라 소비자가 늘면 Redis·advisory lock으로 옮긴다. 일정 변경 채택: D6 → 프론트 연결 → 배포 1주(EC2·GitHub Actions)
  2. 질문으로 얻은 것: 동시 요청 2개가 COUNT=2를 함께 보고 둘 다 INSERT → 4개(타임라인). READ COMMITTED 트랜잭션으로 감싸도 상대의 미커밋 INSERT가 안 보여서 못 막는다. "한 이메일 15분 3개"는 여러 행 조건이라 CHECK로 이중화할 수 없다 — 그래서 `MagicLinkPolicy`가 Domain이 아닌 Application에 있다. "단일 인스턴스라 괜찮다"는 A1·A2 둘 다에 해당해 둘을 가르는 근거가 아니다. 발송 지점에서 세는 방식이 실무에서 드문 이유 = 실무 발송은 소비자가 여러 개라 무경합 전제가 없다. A3만으로 단일 IP 폭주는 막히고, 남는 건 여러 IP의 분산 공격
  3. 핵심 개념: `[NotNullWhen]`은 메서드 **반환값**에 묶인 약속 — `error != null`을 따로 검사해도 컴파일러는 `email`을 추론하지 못하니 `if (!TryGetValue(...)) return error;`로 분기한다. 만들고 버린 반환값(`Result.Failure(error);`, `return` 없음)은 경고 없이 컴파일된다 → 테스트가 필요한 이유. 고정 시계는 "시각을 두 번 읽는 실수"를 못 잡는다 → 부를 때마다 움직이는 시계. 경계 테스트는 양쪽(2개·3개)을 다 둬야 한쪽 방향 변이를 모두 잡는다
- 막혔던 것 / 어떻게 풀었나: 첫 구현에서 `_ =`로 형식 검사 결과를 버림·`AddAsync` 미`await`·형식 오류를 `throw`로 처리 → 리뷰 질문으로 수정. 예측 ✗: "Fake 시계와 실제 시각이 어긋나 테스트가 실패한다" — 핸들러는 주입받은 시계 말고는 시각을 얻을 방법이 없어 어긋날 수 없었다. 어긋날 수 있는 건 같은 시계를 여러 번 부를 때뿐
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `HandleAsync`의 Q2 속도 제한·리팩터링(사용자 요청 — 본문 초안·INSERT→Enqueue 순서는 사용자), `Nook.Application.Tests`·핸들러 테스트 7개(D4부터 테스트는 Claude 작성, 사용자는 예측), 일정 변경 문서 반영. 결정은 사용자: 일정 변경(D6 → FE → DEP 1주, EC2·GitHub Actions), TOCTOU 방어 ② A3 + Q2 + A1
- 내일 확인할 것:
  - AU-02 consume
  - A1·A3 미구현(DEP 5일째) — 지금은 Q2만 동작

---

### D5-4 (2026-10-20) — AU-02 매직 링크 소모, 핸들러가 쥐는 트랜잭션
- 배운 것 (Claude가 세션 기록으로 정리 — 사용자 검토 필요):
  1. 내가 결정한 것: 트랜잭션 경계는 핸들러가 쥔다((가)) — "회원이 없으면 닉네임 필요"는 업무 규칙이라 Application에 있어야 하고, D6 그룹 합류도 같은 방식으로 쓸 수 있다. 응답은 로그인 200 `{accessToken}` · 닉네임 필요 200 `{needsNickname}` · 무효 링크 400 · 닉네임 중복 409. 같은 이메일 동시 가입은 500 수용(같은 사람, 한쪽은 로그인됨, 데이터 손상 없음)
  2. 질문으로 얻은 것: 무효 링크를 401로 하면 D2-1의 `ErrorType`(Unauthorized 없음)과 프론트의 "401 = 토큰 만료 → 다시 로그인" 공통 처리가 겹친다 — 상태 코드는 클라이언트와의 계약. Q1 전에는 이메일을 모르니 가입자인지 알 수 없다 → 닉네임은 "회원 없음"일 때만 검사. "싼 검사 먼저"는 **언제나 필요한 검사**일 때만 앞으로 당긴다. 사용자 입력에 `From`(빈 값만 검사)을 쓰면 검증을 우회한다
  3. 핵심 개념: `await using var tx` — 커밋하지 않고 빠져나가면 Dispose가 롤백한다. 그래서 "닉네임 필요"는 성공 결과인데도 커밋하지 않아 Q1 소모가 되돌아가고 같은 토큰으로 재시도할 수 있다(통합 테스트 `consumed_at IS NULL`로 확인). 저장소·UnitOfWork가 Scoped라 같은 요청의 같은 `DbContext`를 받아 한 트랜잭션에 묶인다. 실패를 먼저 `return`하는 가드 절로 쓰면 공통 꼬리(Q3 → 커밋 → 반환)가 한 곳에 모인다
- 막혔던 것 / 어떻게 풀었나: VS "메서드 생성"이 `SharedKernel/Result.cs`에 Application 타입을 넣음(원인은 Claude 안내의 잘못된 `Result.Success(...)` 표기) → `git status`의 예상 밖 `M`으로 발견, `git restore`. 핸들러 첫 구현에서 기존 회원 경로 누락·`return` 누락(D5-3과 같은 패턴)·시각 두 번 읽기. 통합 테스트 전체 실패 → 안 바꾼 테스트까지 실패해서 공통 기반 의심 → Docker 엔진 꺼짐
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): AU-02 뼈대 전체(`IUnitOfWork`·`EfUnitOfWork`·`IUserRepository`·`UserRepository`·Q1·Q3·`AuthErrors`·`UserErrors.NicknameTaken`·결과 타입·컨트롤러·DI — 협업 영역 뼈대), 핸들러 수정·리팩터링(사용자 요청 — 초안은 사용자), 통합 테스트 11개(D4부터 테스트는 Claude, 사용자 예측 2/2), 이 "배운 것" 초안(사용자 요청)
- 내일 확인할 것:
  - `WebApplicationFactory` API 테스트 기반(D5 게이트)
  - 같은 이메일 동시 가입이 실제로 교착(40P01)이 나는지 — D6에서 재현

---

### D5-5 (2026-10-21) — WebApplicationFactory API 테스트, PL-01 장소 확정
- 배운 것:
  1. PL-01 동시 등록은 C′(Find → `INSERT ... ON CONFLICT DO NOTHING` → 0행이면 Find)로 결정, 흐름은 저장소에 숨기지 않고 핸들러에 둔다. `DO UPDATE`는 기각 — 클라이언트 입력으로 저장값을 덮어쓰면 안 되고(입력 불신), 같은 장소를 다시 고를 때마다 dead tuple이 쌓인다. 0행 뒤 Find가 반드시 1행인 근거 세 가지: ① ON CONFLICT는 미커밋 상대를 기다렸다가 커밋된 경우에만 0행 ② Read Committed는 문장마다 새 스냅샷 ③ 장소는 삭제하지 않는다. 변이(`ON CONFLICT` 삭제)로 23505가 나는 것까지 확인 (Claude 정리 — 사용자 요청, 결정은 사용자)
  2. 별 상태는 서버가 계산한다(A안, `StarRule.Evaluate` 직접 작성) — 원본은 같은 규칙이 4곳에 흩어져 있어 규칙이 바뀌면 4곳을 고쳐야 했다. 서버 한 곳이면 배포 한 번. JSON은 문자열(`"gray"`)이고 확장 가능 값이라 클라이언트는 모르는 값을 empty로 다룬다. 북마크 API는 토글 대신 PUT/DELETE(멱등 — 따닥 클릭이 상태를 뒤집지 않음), "이미 그 상태"는 409가 아니라 200 + `changed: false`, 장소 없음은 FK 위반(23503)을 잡지 않고 먼저 SELECT → 404 (Claude 정리 — 사용자 요청, 결정은 사용자)
  3. EF `FromSql`은 `ComplexProperty`(Coordinate)의 `HasColumnName`을 무시하고 `Coordinate_Latitude`를 찾는다(LINQ는 정상) — 탐침 P1 LINQ ✅ / P2 순수 SELECT ❌ / P3 별칭 ✅로 "RETURNING 탓"이 아니라 `FromSql` 자체임을 좁힘 → `TryAddAsync`는 id만 받고 넘긴 `place`를 돌려준다(B안). D7 근처 검색처럼 places를 SQL로 읽을 때 다시 만난다 (Claude가 기록 — 사용자 요청)
- 막혔던 것 / 어떻게 풀었나: 예측 ✗ "없는 경로는 404" → 401(인가 미들웨어가 엔드포인트 null에도 Fallback 적용, 404는 파이프라인 끝의 기본값). PL-01 초안의 null place INSERT를 CS8604 경고가 잡음. `Coordinate_Latitude` 실패는 탐침으로 범위를 좁혀 해결(위 3)
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): `NookApiFactory`·API 테스트, PL-01 뼈대·핸들러 수정·`TryAddAsync` B안 수정(사용자 요청), PL-01·StarRule 테스트(D4부터 테스트는 Claude), usecases 1a 문구(사용자 요청), BM 뼈대(협업 영역). 사용자 작성: PL-01 핸들러·`TryAddAsync` 초안, `StarState`·`StarRule`
- 내일 확인할 것:
  - BM-01 핸들러 마무리(`Guid.Empty` 경로 처리 결정) → BM-02 → 테스트
  - ADR 후보: "장소는 삭제하지 않는다" 불변식

---

### D5-6 (2026-10-22) — BM-01·02 북마크, LK-01 좋아요
- 배운 것:
  1. 멱등 API는 "이번 요청이 상태를 바꿨나(`changed`)"와 "지금 그 상태인가(`bookmarked`)"가 다른 질문이다. 저장이 성공으로 끝나면 changed와 무관하게 bookmarked=true, 해제면 false. 별 상태는 changed가 아니라 bookmarked로 계산한다 — BM-01 초안은 둘을 한 값으로 써서 재저장 시 star=empty가 나왔다 (Claude 정리 — 사용자 요청)
  2. `x is { } y`는 "null이 아니면"이라 nullable이 아닌 `bool`에는 항상 참이다. PL-01(`Place?`)에서 맞던 관용구가 반환 타입이 바뀌면 다른 뜻이 된다. 변이 실험에서 `Remove_WhenNotBookmarked`는 changed가 원래 false라 `bookmarked: changed` 변이를 못 잡았다 — 테스트 입력이 두 값을 갈라 놓아야 변이가 드러난다 (Claude 정리 — 사용자 요청)
  3. 경로의 `Guid.Empty`는 500으로 둔다(사용자 결정) — 라우트 제약 `{placeId:guid}`는 통과하지만 정상 클라이언트가 보낼 수 없는 값이라 `PlaceId.From`의 예외(개발자 실수·손상 취급)에 맡긴다. 404 `PlaceNotFound`로 바꾸면 "없는 장소"와 "있을 수 없는 ID"가 섞인다. TestServer는 예외 처리 미들웨어가 없으면 500이 아니라 예외를 던지므로 운영 응답 모양은 DEP에서 확인 (Claude 정리 — 사용자 요청, 결정은 사용자)
  4. 같은 "사전 확인 → 쓰기" 모양도 **확인한 대상이 사라질 수 있는가**에 따라 안전성이 갈린다. 장소는 삭제가 없어(불변식) 틈이 무해하지만, 리뷰는 RV-03·탈퇴 CASCADE로 지워져 틈이 실제 경합이 된다. `ON CONFLICT`는 UNIQUE·PK 충돌만 받고 FK 위반(23503)은 그대로 올라와 500 → 사전 확인을 유지하고 `fk_likes_reviews` 위반만 404로 번역(A안). 사전 확인 없이 FK만 잡는 B안은 D9에서 트랜잭션에 묶이면 오류 뒤 25P02로 계속 못 하고, 회원 FK(탈퇴 후 남은 토큰)와 섞이며, DELETE에는 FK 신호가 없어서 바꿨다 (Claude 정리 — 사용자 요청, 결정은 사용자)
  5. 같은 틈이라도 **응답이 거짓이 되는가**로 번역 여부를 정한다. 좋아요 중 리뷰 삭제 → `liked:true`는 거짓이라 404로 번역, 취소 중 리뷰 삭제 → CASCADE가 먼저 지워 `liked:false`는 사실이라 200 허용. enum 결과를 묶을 때도 출력(changed=false)이 아니라 의미(성공/실패)로 묶는다 — AddLike 초안이 `ReviewGone`을 `AlreadyLiked`와 묶어 200을 냈다 (Claude 정리 — 사용자 요청, 결정은 사용자)
- 막혔던 것 / 어떻게 풀었나: BM-01 초안 리뷰 🔴 3개 — `ExistsAsync` 조건 반전(`!` 누락), 괄호 오류, `bool`에 `is { } x` 패턴(항상 참 → changed 항상 true, 재저장 시 star=empty). 원인은 "이번에 바꿨나(changed)"와 "지금 그 상태인가(bookmarked)"를 한 값으로 씀. BM-02에서는 구분해 작성. 예측: 동시 저장 진 쪽 0행 ✅(보완: 미커밋 상대를 PK에서 기다림), 변이 `bookmarked: changed` → 2개 실패 ✅. LK: 처음엔 B안(FK 위반만 잡기)을 골랐다가 대가를 보고 A안으로. AddLike 초안 🔴 `ReviewGone` → 200(직접 수정)
- Claude가 대신 작성한 부분 (있다면, 이유와 함께): BM-01·AddLike 리팩터링(사용자 요청 — 초안은 사용자), BM-02·LK `TODO(human)` 틀, LK 뼈대(`ILikeRepository`·`AddLikeOutcome`·`LikeRepository` 23503 번역·컨트롤러·DI), `BookmarksApiTests` 9개·`LikesApiTests` 10개(D4부터 테스트는 Claude, 사용자 예측 2/2 + LK Q1·Q2 ○)·변이 실험 실행, 커밋, `progress.md` 갱신, "배운 것" 정리(사용자 요청). 사용자 작성: BM-01 초안, BM-02, AddLike 초안·🔴 수정, RemoveLike. 결정은 사용자: 경로 `Guid.Empty` → 500, LK A안·취소 404·틈 사이 취소 200·BM 유지
- 내일 확인할 것:
  - LK 변이 실험 예측(catch 제거 시 어떻게 실패하나) → RV-01
  - ADR 후보: "장소는 삭제하지 않는다" 불변식 — 결과 절에 "깨지면 BM도 LK처럼 23503 번역 필요"

---

### D5-7 (2026-10-23) — 테스트 거짓 통과(incident 001), RV-01 작성, RV-02 도메인
> 이 항목은 Claude가 작성했다(2026-10-25 사용자 요청 — 예측이 틀린 곳과 사용자 결정 위주).
- 배운 것:
  1. **시간으로 기다림을 단언하면 거짓 통과가 생긴다.** LK-01의 catch를 지워도 테스트가 5/20 통과했다. 첫 요청 준비 비용(~400ms)이 500ms 기준과 경합했기 때문이다. 잠금 대기를 `pg_blocking_pids`로 직접 관찰하는 `LockWait`(B안, 사용자 결정)로 바꾸자 0/20 → incident 001
  2. **"지금"은 한 번만 정한다(ADR-007).** RV-01 초안은 시각을 세 번 읽었고 `GetLocalNow`를 썼다. KST 00~09시에는 사전 확인 날짜와 UNIQUE 날짜가 갈린다. 테스트 시각을 20:00Z(KST 다음 날 05:00)로 잡아 로컬 날짜로 바꾸는 변이를 잡았다
  3. **사전 검사는 정확성이 아니라 비용(고아 이미지)을 줄이는 장치다.** 그래서 테스트는 응답 코드와 함께 "스토리지에 무엇이 남았나"를 단언해야 순서가 틀린 구현을 잡는다
  4. EF가 복원한 `_images`의 물리적 순서는 보장되지 않는다. 순서는 `keepImageIds` 안의 인덱스로 매긴다
- 테스트 예측이 틀린 곳:
  - 정상 작성 응답을 202로 예측 → 실제 201(ADR-009 결정 2)
  - 이미지 4장 요청에서 스토리지 1개로 예측 → 실제 0개. 장수 검사가 형식 판별보다 앞이라 하나도 올라가지 않는다
  - 6a(동시 작성) 스토리지 개수 → 어느 쪽이 사전 확인을 통과하는지가 매번 달라 비결정. `RendezvousStorage`로 "둘 다 통과" 경로를 고정했다
  - `Review.Edit` 단위 테스트 6개는 모두 적중
- 사용자 결정: LockWait B안 · D6 게이트 "incident 1건 이상" · RV-02·03·BM-03·PL-02를 D5에 포함 · 이미지 정리는 D3 결정 유지(커밋 후 삭제 + 배치) · RV-01 201 · 남의 리뷰 403 · RV-03 C안(`star=null`) · RV-02 순서 계약 "남길 기존 → 새 이미지"
- 막혔던 것 / 어떻게 풀었나: RV-01 초안 🔴 3(장소 조건 반전, "지금" 세 번 + 로컬 시각, 검증과 업로드가 섞임) → 사용자 일부 수정 후 Claude 수정. `Review.Edit` 초안 🔴 3(새 이미지 미추가, 순서 미반영, 중복 ID 허용) → 사용자가 추가·재번호, 나머지 Claude. content null 검사는 "이미 검증됨" 주석을 보고 생략했던 것 — 규칙 검증(Result)과 null(버그 → 예외)은 다른 질문이라 가드 추가
- Claude가 대신 작성한 부분: `LockWait`, incident 001, RV 유스케이스 수정·보완, ADR-009, RV-01 뼈대·핸들러 수정·테스트, `Review.Edit` 수정·테스트, RV-02 뼈대·`ReviewImageUploads`
- 내일 확인할 것: RV-02 핸들러 작성

---

### D5-8 (2026-10-24~25) — RV-02 동시 수정 데드락(incident 002), RV-03·BM-03·PL-02, D5 기능 완료
> 이 항목은 Claude가 작성했다(2026-10-25 사용자 요청 — 예측이 틀린 곳과 사용자 결정 위주, 데드락은 반드시 포함).
- 배운 것:
  1. **DEFERRED 제약은 검사를 미룰 뿐 없애지 않는다.** 미뤄진 검사는 COMMIT에서 상대 트랜잭션을 기다리고, 그 대기는 코드 어디에도 보이지 않는다. 다른 락(본문 UPDATE의 행 잠금)과 엮이면 데드락이 된다
  2. **충돌을 "0행·유니크 위반" 같은 부수 효과로 감지하면 빈틈이 있다.** EF는 바뀌지 않은 엔티티에 문장을 보내지 않으므로, 신호가 하나도 안 나는 조합이 생긴다. 버전 토큰은 신호가 반드시 생기게 한다
  3. **잠금과 버전은 하는 일이 다르다.** 잠금은 "언제 실행할지"(한 줄로 세움), 버전은 "실행해도 되는지"(읽은 뒤 바뀌었나)를 정한다. 하나만으로는 데드락이나 섞인 저장 중 하나가 남는다
  4. **한 결함을 고치면 그 뒤에 숨어 있던 결함이 드러난다.** 데드락(500)이 섞인 저장을 가리고 있었다. 고친 뒤에도 같은 테스트를 반복 실행해야 한다
  5. **무한 스크롤에 OFFSET은 결과가 틀린다(성능 이전의 문제).** 첫 페이지 뒤에 새 글이 생기면 한 칸씩 밀려 본 항목이 다시 나온다. 불투명 커서 + 키셋 `(created_at, id)`로 만들면 나중에 방식을 바꿔도 클라이언트 계약이 그대로다
- 테스트 예측이 틀린 곳과 그 뒤의 결정:
  - **RV-02 동시 추가(9번): "둘 다 200"으로 예측 → 의도는 한쪽 409, 실제로는 3/10 확률로 500(40P01 deadlock)**
    - 가설 1(사용자): "t1 INSERT가 잠그고 t2 INSERT가 대기 → t1 커밋 → t2 유니크 위반". psql 실험 A로 검증 → **틀림.** DEFERRED라 INSERT는 기다리지 않고, 검사가 COMMIT으로 미뤄져 23505가 난다. 이건 409로 바꾸는 정상 경로라 데드락을 설명하지 못했다
    - 실험 B(본문 UPDATE 추가): 처음 결과가 이상했다 — 실험 A가 남긴 행 때문이었다. **동시성 실험은 초기 상태부터 확인해야 한다**
    - 실험 C(INSERT 순서만 바꿈): 40P01 재현. 근본 원인: 나중에 INSERT한 쪽이 커밋 때 DEFERRED 재검사로 상대를 기다리고, 상대는 이쪽이 쥔 reviews 행(본문 UPDATE)을 기다린다
    - **결정: ① 리뷰 행을 먼저 잠근다(`FOR UPDATE`)** — 40P01을 409로 덮는 ②, 재시도 ③은 원인을 남기고 최소 1초 지연이라 기각
  - **잠금 뒤 새 결함: 남길 이미지가 다른 두 수정이 둘 다 200, DB에는 `[n1@1, n2@2]`(5/20)**
    - "늦은 쪽이 보내는 문장"을 물었을 때 사용자는 의도(A 유지, B·C 삭제, n1 추가)로 답했다. 실제 문장은 A 없음(변경 없음), B·C DELETE 1행씩(행은 남아 있음), n1 INSERT 성공 — 신호가 하나도 없었다
    - **결정: (나) `reviews.version` 낙관적 동시성 토큰.** 이미지 목록 비교(가)는 본문만 바꾼 충돌을 놓쳐 기각. 결과 0/30 실패, 토큰 제거 변이는 6/15만 잡음(누가 먼저 커밋하는지 비결정)
  - **RV-02 8번(서로 다른 keep): 진 쪽 409는 맞음.** 남는 이미지 수는 누가 이기느냐에 따라 달라 "DB 이미지 + 진 쪽 고아 1장 = 스토리지"로 단언을 바꿨다
  - RV-02 1~7번, RV-03 9번(삭제 후 오늘 다시 작성 가능)은 적중
- 그 밖의 사용자 결정:
  - 동시 수정의 진 쪽: 404와 구분해 **409 `Review.EditConflict`**((b))
  - 응답 이미지: **`images: [{ id, key }]`**((A)) — 키는 DEP에서 URL로 바뀌므로 식별자로 쓰지 않는다. ADR-009 결정 2 개정
  - incident 002 미결 2건(테스트 결정성, `version` + `If-Match`)은 D6에서
  - BM-03: `PlaceView + star`, D5는 상한 없이 전체(D8 측정 후 결정), 쿼리는 Claude 작성
  - PL-02: 좋아요 수·내 좋아요·작성자 닉네임·내 리뷰 여부 포함, 무한 스크롤
- 막혔던 것 / 어떻게 풀었나:
  - RV-02 핸들러 초안 🔴 3 — 작성자 확인(4b) 없음, 업로드가 조회보다 먼저(거절돼도 고아 이미지), 커밋 후 삭제 예외 미처리(성공한 수정이 500). 순서를 "키 생성 → `Edit` → 업로드"로 바꿔 모든 거절을 업로드 전에 끝냈다
  - RV-03 초안 🔴 1 — 삭제 후 `hasReview`를 `false`로 고정. R-RV-03은 "하루 1개"라 다른 날 리뷰가 남을 수 있다 → 사용자가 수정, 변이로 테스트가 잡는 것 확인
  - BM-03 주석 "UNION이 중복을 지운다"가 틀렸다 — UNION ALL로 바꿔도 통과. 장소당 1행은 `IN`(세미 조인)이 보장한다. 변이는 설명의 오류도 드러낸다
- Claude가 대신 작성한 부분 (이유: 사용자 요청): RV-02 핸들러 리팩터링(4b·순서·삭제 처리), `ReviewGone`·`Conflict` 처리, 행 잠금 + `Review.Version`·`AddReviewVersion` 마이그레이션, incident 002, 응답 이미지 ID 변경, RV-03 뼈대·CTE 삭제 쿼리·`ReviewImageCleanup`, BM-03·PL-02 전체, 모든 테스트와 변이 실험, 이 학습 로그. 사용자 작성: RV-02 핸들러 초안, RV-03 핸들러(판정·별 계산), psql 실험 A·B·C 실행
- 내일 확인할 것:
  - `feat/d5-auth` 푸시·PR·병합(사용자)
  - D6 시작: GR-01 5명 동시 합류 재현. incident 002 미결 2건을 함께

---

### D6-1 (2026-10-25) — 그룹 동시성 4건: 합류 정원·생성 원자성·참여 3개·마지막 멤버 탈퇴
> 이 항목은 Claude가 작성했다(2026-10-26 사용자 요청 — 사용자 결정과 핵심 위주).
- 배운 것:
  1. **잠금은 판단 재료를 최신으로 만들 뿐, 판단은 도메인이 한다.** 정원 4명은 여러 행에 걸친 규칙이라 CHECK로 못 박는다. 그룹 행을 `FOR UPDATE`로 잠근 뒤 멤버를 읽고 `Group.AddMember`가 판단한다
  2. **잠그는 문장과 읽는 문장을 나눠야 한다(incident 003).** `FOR UPDATE`를 JOIN 한 문장에 넣었더니 여전히 6명. 잠금 대기 뒤 최신으로 다시 보는 것은 잠긴 groups 행뿐이고, JOIN한 멤버는 대기 전 스냅샷이었다
  3. **같은 잠금을 쓰는 쪽끼리만 줄을 선다.** 같은 회원의 합류·생성이 다른 그룹이면 그룹 행 잠금으로는 못 막는다 → 공통 행은 users 행. 합류의 users 잠금을 빼면 생성 쪽 잠금만으로는 못 막았다(변이 → 5개)
  4. **행 두 개를 잠그면 순서가 곧 안전이다.** 반대 순서 학습 테스트에서 40P01(약 1초 뒤)을 직접 봤다. 그래서 users → groups로 고정
  5. **잠금 강도도 설계다.** users를 `FOR UPDATE`로 잠그면 그 행을 FK로 참조하는 INSERT(`FOR KEY SHARE`)까지 막힌다. `FOR NO KEY UPDATE`는 FK 검사는 통과시키고 자기끼리만 충돌한다
  6. **EF는 `SaveChanges`마다 세이브포인트를 만든다.** 그래서 초대 코드 UNIQUE 위반 뒤에도 users 잠금을 쥔 채 같은 트랜잭션에서 재시도할 수 있다
- 내가 결정한 것:
  - 합류 해결책 **(가) `FOR UPDATE`** — 처음엔 (나) 카운터 컬럼을 골랐다가, erd.md "인원 수 컬럼 없음" 근거와 그룹 목록 유스케이스가 없다는 점을 다시 보고 바꿨다. (다) SERIALIZABLE은 재시도 없이 1 성공 / 4 실패라 R-GR-03(빈자리가 있으면 받아야 함) 위반
  - 초대 코드 충돌은 최대 3회 재시도, 그래도 실패하면 예외(회원 입력 문제가 아니라 생성기 고장)
  - 생성 응답은 `GroupView(id, name, inviteCode)` — 만든 사람이 바로 공유해야 하므로 코드 포함
  - 잠금 순서 users → groups, users 강도 `FOR NO KEY UPDATE`
  - 이미 멤버인 그룹에 다시 합류하면 `TooManyGroups`보다 `AlreadyMember`가 우선
  - 탈퇴는 groups 행만 잠근다(참여 수를 줄이기만 해서 R-GR-02를 못 깬다). 없는 그룹·멤버 아님도 성공(멱등)
- 예측이 틀린 곳:
  - 빈자리 3에 5명 동시 합류 결과를 "3 + 동시 요청 수"로 예측 → 만든 회원 1명을 빠뜨림(실제 멤버 6명)
  - 3개 검사 구현에서 `>` 경계 오류 한 번(3개 참여 중인데 통과)
  - 생성 원자성(없는 회원으로 멤버 INSERT만 FK 실패시키면 그룹도 안 남음)은 적중
- 막혔던 것 / 어떻게 풀었나: GR-02 재시도 루프 초안 — `catch (Exception)`로 재시도(FK 오류까지 재시도), `TryAddAsync` 반환값 무시, 반복 범위 오류 → Claude 수정(사용자 요청). 재시도는 "코드 충돌(false)"만, 다른 DB 오류는 다시 해도 같으므로 예외 그대로
- Claude가 대신 작성한 부분: 모든 테스트와 변이 실험, `GroupRepository` 전체, `UserRepository.LockAsync`, 초대 코드 생성기·`GroupView`, GR-02 핸들러(재시도 루프 수정 포함)·잠금 부분, incident 003·ADR-010·erd.md groups 표기. 사용자 작성: GR-01 핸들러, 합류 users 잠금·3개 검사, 탈퇴 `MemberRemoval` switch
- 내일 확인할 것: incident 002 미결 2건, 그룹 API 엔드포인트

---

### D6-2 (2026-10-26) — 동시성 테스트 결정성, 화면까지 왕복하는 버전, 그룹 API
> 이 항목은 Claude가 작성했다(2026-10-26 사용자 요청 — 사용자 결정, 핵심, `Edit_ConcurrentWithDifferentKeep_LaterSaveReturns409` 코드 학습 포함).
- 배운 것:
  1. **랑데부는 출발선만 맞춘다.** 기존 테스트는 둘 다 조회를 끝낸 상태까지만 고정하고 누가 먼저 커밋할지는 스케줄러에 맡겼다. 그래서 version 토큰 제거 변이를 6/15만 잡았다. 결승선(커밋 순서)은 한쪽을 붙잡아 두는 게이트로 따로 고정해야 한다
  2. **두 커밋 순서가 지키는 것이 다르다.** keep [A] 먼저면 진 쪽의 B `UPDATE sort_order`가 0행이라 version 없이도 409. keep [B, C] 먼저면 진 쪽 문장이 전부 1행이라 version만 잡는다. 변이 결과도 정확히 그랬다(True만 빨강)
  3. **낙관적 토큰은 "결정한 주체가 무엇을 봤는가"의 증거다.** 서버가 저장 직전에 새로 읽은 값은 사람이 본 값이 아니다. 화면을 연 사이의 덮어쓰기를 막으려면 토큰이 화면까지 갔다 와야 한다
  4. **사전 확인 + 최종 판정 2단 구조.** 핸들러에서 화면 버전을 비교해 업로드 전에 거절하고(고아 방지), 통과하면 읽은 값 = 화면 값이라 저장의 `WHERE version = ?`가 그대로 최종 판정을 한다. RV-01의 "사전 확인 + UNIQUE"와 같은 모양
  5. **값을 바꾸는 비용은 그 값의 의미를 누가 아느냐에 비례한다.** version을 "불투명 값(받은 그대로 돌려보냄)"으로 계약하면 나중에 `xmin`으로 바꿔도 서버 안에서 끝난다
  6. **핸들러 테스트와 HTTP 테스트는 지키는 것이 다르다.** 그룹 핸들러가 API DI에 빠져 있었는데 핸들러 테스트는 자기 컨테이너에 등록해서 통과했다. HTTP 테스트를 쓰자마자 드러났다
- `Edit_ConcurrentWithDifferentKeep_LaterSaveReturns409` 코드 학습:
  - **업로드 지점** = `EditReview.cs`의 `ReviewImageUploads.UploadAsync` → `storage.PutAsync`. 테스트가 DI에서 `IReviewImageStorage`를 `OrderedGateStorage`로 바꿔 끼워, 서버 코드를 고치지 않고 실행 흐름에 끼어든다
  - **"둘 다 조회를 마친 뒤 저장"** = 핸들러 순서가 조회(`GetAsync`) → `Edit`(메모리) → 업로드 → 저장(`TrySaveAsync`)이라, 업로드 지점에 둘 다 도착했다는 것은 둘 다 같은 스냅샷(version 0)을 들고 있다는 뜻
  - **`new Barrier(participantCount: 2)`** = 2명이 모여야 열리는 문. `SignalAndWait`는 도착을 알리고 나머지가 올 때까지 멈춘다. 참가 횟수는 요청 수가 아니라 `PutAsync` 호출 수라서 요청마다 새 이미지를 딱 1장만 보낸다. 스토리지가 Singleton이라 두 요청이 같은 Barrier를 공유한다(Scoped였다면 각자 10초 기다리다 타임아웃). `SignalAndWait`는 스레드를 붙잡는 동기 대기라 `Task.Run`으로 감싼다
  - **`TaskCompletionSource` 게이트** = Barrier는 대칭(둘 다 출발), TCS는 비대칭(테스트가 정한 시점에 한쪽만). 테스트가 이긴 쪽의 200(= 커밋 완료)을 받은 뒤 `SetResult`로 늦은 쪽을 푼다. 시간 추측이 없다(incident 001)
  - **단언의 목적**: 이긴 쪽 200(전제 + 커밋 완료) → 늦은 쪽 409(핵심, 변이가 여기서 실패) → 공통 단언: 상태 코드 `[OK, Conflict]`, 409의 에러 코드가 6c, 승자 응답 이미지 = DB(섞인 저장 없음), 스토리지 = DB + 1(진 쪽 새 이미지만 고아), DB의 모든 이미지가 스토리지에 있음(진 쪽이 롤백됐는데 자기 `removedKeys`를 지우면 승자의 사진이 깨진다)
  - 처음 실행 실패는 테스트 결함 — 409 응답 본문을 두 번 읽어 스트림이 닫혀 있었다. 새 테스트가 빨강이면 서버보다 "테스트가 맞게 관찰하는가"를 먼저 의심한다
- 내가 결정한 것:
  - 화면 버전 왕복: **A** 토큰은 `reviews.version`(xmin은 무관한 변경에도 바뀌어 거짓 충돌) · **B** 바디 필드(단건 GET이 없고 목록에서 편집으로 들어가 ETag가 안 맞음) · **C** 409 재사용 · **D** 필수(FE가 아직 없어 깨질 클라이언트가 없음) → ADR-011
  - 버전 비교는 **핸들러, 권한 검사 뒤·업로드 전**에 직접 구현. 남의 리뷰에 409를 주면 "그사이 수정됐다"가 새어 나간다. 화면이 무엇을 봤는가는 유스케이스의 관심사라 도메인에 넣지 않음
  - 그룹 API: 합류 `POST /groups/join` + 본문 코드(경로는 로그에 남음), 합류 응답 `200 + GroupView`, 탈퇴 `DELETE /groups/{id}/membership` 204
  - ADR-010 후속 결정 반영(잠금 순서·강도·탈퇴 잠금)
- 예측이 틀린 곳:
  - keep [A] 먼저 커밋한 뒤 진 쪽의 B 문장을 `INSERT`로 예측 → 실제는 추적 중인 엔티티의 `ChangeSortOrder` → `UPDATE sort_order`. 0행 결과는 적중
  - 변이 실험 결과 예측은 건너뛰고 바로 실행
  - "화면을 연 채 다른 기기가 수정하면 버전이 낮으니 실패" → 실제로는 화면 버전이 서버에 가지 않아 서버가 최신을 읽고 통과, 덮어씀
  - 비교를 조회 직후(41줄) vs 저장 WHERE(58줄) 중 58줄로 답함 — Claude의 질문이 잘못된 양자택일이었다. 41줄 비교를 통과하면 58줄이 같은 값으로 판정하므로 둘 다 필요(41 = 업로드 전 거절, 58 = 최종 판정)
- 막혔던 것 / 어떻게 풀었나: 변이를 되돌릴 때 앞 줄 세미콜론이 남아 빌드 실패 → 원본으로 복원. 그룹 API 테스트 첫 실행 4건 실패 = 시드 시각이 실제 시계보다 미래(`AddMember` 가드)
- Claude가 대신 작성한 부분: `OrderedGateStorage`·Theory 테스트, Version 뼈대(View·Request·Command·컨트롤러)와 테스트, `GroupsController`·DI 등록·합류 핸들러 반환 타입·`GroupsApiTests`, incident 002·ADR-011·ADR-010 후속·usecases.md·erd.md·ADR-009 갱신, 이 학습 로그(사용자 요청). 사용자 작성: 버전 사전 비교, 변이 실행
- 내일 확인할 것:
  - 이월 메모: 그룹 조회 API(내 그룹 목록·상세)가 유스케이스에 없음 — FE 연결 때 필요하면 유스케이스부터
  - `feat/d6-concurrency` 푸시 → PR → `main` 병합

---

### D6-3 (2026-10-26) — D6 마무리, PF-02 회원 탈퇴 미리 보기, PM 확인 항목
> 이 항목은 Claude가 작성했다(2026-10-26 사용자 요청 — 구현 없이 정리만 한 세션이라 PF-02에서 결정할 것·알아야 할 것 위주).
- 한 일: 그룹 조회 API 원본 조사(`getMyGroups`만 유스케이스에 없음 → PM 확인), PF-02 미결 3건 묶음, `progress.md`에 "PM 확인 항목" 절 신설, 빌드·테스트 252개 확인 후 D6 마감
- **PF-02에서 알아야 할 것**
  1. **탈퇴 = users 행 DELETE 한 문장 + 그 앞의 앱 절차.** 자식 테이블의 `ON DELETE`가 나머지를 정한다

     | 자식 FK | 동작 | 탈퇴 때 일어나는 일 |
     |---|---|---|
     | `bookmarks.user_id` | CASCADE | 같이 지워짐 |
     | `reviews.user_id` | CASCADE | 같이 지워짐 → `review_images`·`likes.review_id`도 CASCADE로 연쇄 |
     | `likes.user_id` | CASCADE | 같이 지워짐 |
     | `group_members.user_id` | **RESTRICT** | 멤버십이 남아 있으면 DELETE 거부 |

  2. **RESTRICT는 일부러 건 안전장치다.** CASCADE면 탈퇴한 회원이 마지막 멤버였던 그룹이 0명으로 남는다(R-GR-09 위반, 원본의 고아 그룹 결함). 그래서 순서가 정해져 있다: 참여 그룹마다 `Group.RemoveMember` → `GroupEmptied`면 그룹 삭제 → users DELETE. 순서를 빠뜨리면 RESTRICT가 오류로 드러낸다(ADR-002·004, erd.md group_members FK)
  3. **CASCADE도 공짜가 아니다.** users DELETE 한 번이 자식 테이블마다 `WHERE user_id = ?`를 실행한다. PostgreSQL은 FK 쪽 인덱스를 자동으로 만들지 않으므로 자식마다 `user_id`로 시작하는 인덱스가 있어야 전체 스캔을 피한다(erd.md users "FK (들어오는 것)"). 확인은 D8 시드 뒤 `EXPLAIN`
  4. **스토리지 이미지는 DB 트랜잭션 밖이다.** 리뷰 행은 CASCADE로 지워져도 스토리지 객체는 남는다. 트랜잭션 안에서 외부 I/O를 하지 않는다는 원칙 때문에 정리 배치 몫인데, 배치는 최근 prefix만 훑는다(D3-2) → 오래된 탈퇴 회원 이미지는 주 1회 전체 스캔에 기대야 한다
  5. **무상태 JWT의 대가.** 액세스 토큰은 60분(`JwtOptions.AccessTokenMinutes`)이고 서버가 무효화할 수 없다. 탈퇴 직후 그 토큰으로 좋아요·북마크를 요청하면 없는 회원 ID로 INSERT → FK 위반(23503) → 지금은 500. D5-1에서 "상태 없는 JWT는 무효화를 잃는다"고 정리한 것이 구체적인 증상으로 나타나는 곳
- **PF-02 착수 전에 정할 것** (`progress.md` "미결 결정"의 묶음, 일정에 넣을지는 PM 확인 항목 3)
  - ① 탈퇴 트랜잭션도 처음에 users 행을 잠그는가 — 안 잠그면 탈퇴 도중 같은 회원의 합류가 끼어들어 RESTRICT 오류. 데이터는 RESTRICT가 지키므로 정합성이 아니라 **실패를 어떻게 보여 줄지**의 문제. 잠근다면 순서는 합류와 같은 users → groups(교착 방지). 근거: 잠금은 같은 잠금을 쓰는 쪽끼리만 줄을 세운다(D6-1 변이)
  - ② 탈퇴 후 남은 토큰의 FK 위반을 500으로 둘지, 401·404로 번역할지, 토큰 무효화 수단(블랙리스트·짧은 수명)을 둘지
  - ③ 탈퇴 회원의 오래된 이미지를 어떻게 지울지 — "모든 데이터 삭제, 복구 불가"를 만족하려면 전체 스캔을 기다리는 걸로 충분한가
- 내가 결정한 것: PF-02 잠금 결정은 **PF-02 구현 때로 미룸**(일정에 없고, 미루는 비용이 핸들러 첫 줄 하나라 작음). 그룹 조회·PF-02 범위는 **PM과 의논**
- 배운 것(과정): 유스케이스 목록을 원본의 **쓰기 함수 위주**로 뽑으면 화면 진입용 읽기(`getMyGroups`)가 빠진다. 혼자 정할 수 없는 질문은 "미결 결정"과 섞지 말고 "PM 확인 항목"으로 따로 모아야 다음 세션에서 막힌 이유가 보인다
- Claude가 대신 작성한 부분: 원본 조사 표, `progress.md` 정리, 이 학습 로그(사용자 요청)
- 다음에 할 것:
  - `feat/d6-concurrency` 푸시 → PR → `main` 병합
  - PM과 "PM 확인 항목" 1~4 의논 → FE 시작

---

## 2026-10-27~28 (FE-2) — 형식만 Claude가 잡음, 문장은 사용자 작성

- 오늘 배운 것 3줄:
  1. (사용자 작성)
  2. (사용자 작성)
  3. (사용자 작성)
- 막힌 것: (사용자 작성) — 참고: 그룹 지도 버그 → 아래 FE-3에서 원인 확정·해결(incident 004)
- Claude가 대신 쓴 것(사용자 요청 — 면접·역량검사 대비 기간): 서비스 계층 핸들러 전부, 도메인·마이그레이션(성별·생일·사진·방장·색 슬롯·탈퇴 SET NULL·좋아요 대리 키), 테스트 전부, web/ BFF·화면 교체 전부, ADR-012, 유스케이스 줄(AU-04·RV-04·GR-06·R-US-04·05·R-GR-11·12), 백로그 문서
- 면접 대비로 다시 볼 만한 결정(Claude 제안 목록): BFF + 허용 목록 중계(open proxy 방지), 매직 링크 `#token`, R-RV-03 UTC→KST(CHECK를 안 둔 덕에 싸게 바꿈), 좋아요 복합 PK → 대리 키, FK 위반 → 401 번역과 "버그를 401로 가린" 변이, N+1 → SQL 1문장(로그로 확인)

---

## 2026-10-28 (FE-3) — 그룹 지도 회색 별 버그, FE 마무리(PR #9)

> 이 항목은 Claude가 작성했다(2026-10-28 사용자 요청).

- 한 일: 그룹 지도 버그 재현 → 원인 확정 → 수정 `0bf1e24` → incident 004 · 브라우저 확인 결과 기록(이전 A1~9·B10~16 전부 정상, 회귀용 목록 `docs/design/fe-manual-checklist.md` 재구성) · `dotnet build` 경고·오류 0, `dotnet test` 348개 통과, `next build` 성공 → PR #9 병합
- 오늘 배운 것 3줄:
  1. **응답 경계로 문제를 반으로 자른다.** BFF 응답을 브라우저로 직접 열어 `bookmarkCount:1`을 보는 한 번의 관찰로 DB·SQL·API·BFF·인증 후보가 한꺼번에 빠졌다(H1 기각). 남은 쪽(그리기 H2 / 다시 불러오기 H3)은 "다시 불러오기"만 바꾼 대조 실험(그대로 → 없음, 바꿨다 돌아옴 → 보임)으로 갈랐다
  2. **증상이 같아도 조건이 바뀌면 원인 후보가 바뀐다.** 재현 상대가 슬롯 2(jamME) → 0(dong)으로 바뀌면서 "0은 JS falsy" 후보가 새로 생겼고, 데이터로 따로 기각했다. 처음 보고의 "리뷰를 쓰면 채운 별로 보인다"는 회색 단계를 건너뛴 **첫 다시 불러오기**였다 — 별 규칙은 맞고 화면이 낡았던 것
  3. **"언제 다시 불러오는가"는 설계 항목이다.** 원본 nook도 realtime·복귀 갱신이 없어 물려받은 공백. 서버를 빠르게 해도 줄지 않는 지연(처음 보고 최소 47분). `visibilitychange`만으로는 창 두 개를 나란히 띄운 재현 환경에서 동작하지 않아 `focus`를 함께 걸고, 두 이벤트가 동시에 오는 탭 복귀는 1초 중복 방지 → Network 탭 실측 1건
- 막힌 것: 지난 세션의 브라우저 확인 목록(A1~9·B10~16)이 대화에만 있어서 사라졌다 → 파일로 다시 만들었다. 재현 계정(jamME)도 탈퇴 테스트로 지워져 새 그룹·다른 계정(dong)으로 다시 세팅했다
- 내가 결정한 것: 해결 A안(화면 복귀 시 다시 불러오기). 버린 것 — B 폴링(변화 없는 요청, 늦은 응답 덮어쓰기 경합 증가), D SSE/SignalR(새 인프라, 범위 밖), E 방치
- Claude가 대신 작성한 부분: `MapScreen.tsx` 수정, incident 004, 수동 확인 목록, `progress.md`, 이 학습 로그(사용자 요청)
- 남은 빈 곳: `web/` 테스트 러너 없음(DEP에서 CI와 함께 결정), 1초 중복 방지 변이 확인 안 함, `reloadSaved` 취소 장치 없음(그룹을 빠르게 바꿀 때 늦은 응답이 덮어쓸 수 있음)
- 기준선(DEP Next 업그레이드 뒤 비교용): First Load JS 공통 87.3 kB, `/review/new` 96.3 kB, Middleware 26.6 kB
- 다음에 할 것: DEP 1일째 — 순서 결정(Next 보안 업그레이드 먼저 vs CI 골격 먼저)

---

## 2026-11-05 (DEP-1) — 배포 계획, Next 16 보안 업그레이드

> 형식과 사실 항목은 Claude가 잡음. "배운 것"·"막힌 것"은 사용자 작성.

- 한 일: DEP 계획 10단계·AWS 보안 Pillar 점검표(`docs/design/dep-plan.md`) · 배포 결정 6개 · Next 14.2.35 → 16.4.5(운영 의존성 취약점 critical 1·high 3·moderate 6 → 0) · `/_next/image` 외부 URL 차단
- 오늘 배운 것 3줄:
  1. (사용자 작성)
  2. (사용자 작성)
  3. (사용자 작성)
- 막힌 것: (사용자 작성)
- 내가 결정한 것: 프론트 같은 EC2, DB 컨테이너, 사진 S3, GHCR→ECR, SSH→SSM, 도메인 구매, Next 업그레이드 먼저
- Claude가 대신 작성한 부분: dep-plan.md, web/ 업그레이드 코드 전부, `next.config.js` 이미지 설정(Next.js 학습 전이라 위임), progress 갱신
- 면접 대비로 다시 볼 만한 것(Claude 제안): 장기 비밀값 0개 설계(OIDC·인스턴스 역할·ECR·SSM), 컨테이너 이미지 vs 업로드 사진의 수명 주기, 도메인이 매직 링크 신뢰 사슬(DKIM·TLS)에 걸린 이유, 쓰지 않는 기능(`remotePatterns: "**"`)이 곧 공격 표면
- 다음에 할 것: 브라우저 확인 시점·커밋 결정 → 2단계 컨테이너화

---

## 2026-11-07 (DEP-2·3) — 컨테이너화, CI 게이트

> 형식과 사실 항목은 Claude가 잡음. "배운 것"·"막힌 것"은 사용자 작성.

- 한 일: 1단계 커밋·PR #10 · 2단계 컨테이너화(api·web Dockerfile, `deploy/` compose·Caddy, migrate 번들 컨테이너) → compose 브라우저 확인 → PR #11 · ADR-003 · 3단계 CI(4 job) → Actions 첫 실행 통과 · Trivy HIGH 해소(chiseled·npm 제거) · lint 오류 5건 수정 · 커밋 전 스테이징 경고 훅
- 오늘 배운 것 3줄:
  1. (사용자 작성)
  2. (사용자 작성)
  3. (사용자 작성)
- 막힌 것: (사용자 작성)
- 내가 결정한 것: 구현체를 설정값으로 선택(fail-fast) · 브라우저 확인은 compose에서 · 결정 6(SG는 내 IP만 → 7단계 뒤 공개, 개인정보 동의 포함) · lint 오류 = CI 실패 · `main` 보호는 CI 표시 + 로컬 훅 · 백로그 X1·X2 · 훅은 플러그인 대신 settings
- Claude가 대신 작성한 부분: Dockerfile·compose·Caddyfile, `Program.cs` Provider 분기, `origin.ts`, **ADR-003(사용자 요청)**, CI·Dependabot·훅, lint 수정(MapScreen·ProfileScreen·magic-link), dep-plan·progress 갱신
- 사고·교훈(사실): ① dev(`npm run dev`)와 compose가 같은 3300에 IPv6/IPv4로 함께 떠 첫 A·B 확인이 dev로 갔다 — DB 행 수(compose users 0)로 발견 ② 앞서 스테이징한 파일이 lint 커밋(`06e26a4`)에 섞였다 → 훅 + `git commit -- <경로>` ③ 토큰 마스킹 정규식 실수로 로컬 테스트 토큰이 출력에 노출(15분 1회용, 로컬)
- 면접 대비로 다시 볼 만한 것(Claude 제안): 이미지(코드) vs 볼륨(상태)의 수명 주기, migrate를 앱 시작과 분리한 이유, 비루트 + 볼륨 소유권, "버전 올리기"보다 "안 쓰는 도구 빼기"(chiseled), fail-fast/fail-closed 설정 설계, effect는 바깥 세계 동기화용(react-hooks v7)
- 다음에 할 것: 4단계 AWS 계정 보안 → 도메인 구매

---

## 2026-11-07 (DEP-4) — AWS 계정 보안

> 형식과 사실 항목은 Claude가 잡음. "배운 것"·"막힌 것"은 사용자 작성.

- 한 일: AWS 가입(새 시작 경험·Free plan) → 결정 7(Paid 업그레이드, 서울) → 고급 기능 활성화(Organizations 3계정, 팀 `nook`) · 루트 비밀번호 생성 + 루트 MFA 2개 · Builder ID MFA 2개 · Budgets 2개 · CloudTrail 조직 트레일 · 리전 제한 SCP에 서울 추가(CloudShell로 API 통과 확인) · 종료·탈퇴 런북
- 오늘 배운 것 3줄:
  1. (사용자 작성)
  2. (사용자 작성)
  3. (사용자 작성)
- 막힌 것: (사용자 작성)
- 내가 결정한 것: Paid 업그레이드 + 서울(시드니 기각 — 지연·개인정보 국외 이전), 에이전트 툴킷 연결 보류, 팀 이름 `nook`, 프로젝트 `nook-prod`, 복구 이메일에 PM 메일 대신 본인 소유, Budgets 50% 추가
- Claude가 대신 작성한 부분: dep-plan 결정 7·§6, `aws-shutdown-runbook.md`(AWS 어시스턴트 답변 정리 + 보충), 이 기록 형식
- 사실·교훈: 새 시작 경험은 Claude가 아는 콘솔과 달라 안내가 여러 번 어긋남 → 화면 문구 우선 · 같은 이메일에 신원 둘(Builder ID / 루트, 비밀번호 별개) · MFA 추가는 MFA로 로그인한 세션 필요(step-up) · "화면이 보인다 ≠ 권한이 있다"(서울 EC2 SCP explicit deny) · 지출 한도의 정체 = Budgets + SCP · SCP는 관리 계정에 적용 안 됨
- 면접 대비로 다시 볼 만한 것(Claude 제안): 다중 계정 구조(관리·신원·작업), 루트 보호와 로그인 경로 세기, IAM 평가 순서(명시적 Deny 우선), `NotAction`+`Deny` 허용 목록 패턴, 차단(fail-closed) vs 알림(fail-open) 비용 통제, 조직 트레일이 멤버 계정 침해에도 남는 이유
- 다음에 할 것: 콘솔 서울 재확인 → S3 퍼블릭 액세스 차단 → Identity Center → CLI SSO → 도메인 구매


## 2026-11-09 (DEP-7) — S3 저장소, IP 요청 제한, 헬스체크, 보안 헤더·CSP

> 형식과 사실 항목은 Claude가 잡음. "배운 것"·"막힌 것"은 사용자 작성.

- 한 일: Dependabot 잔여 PR 정리 · 등록처 Route 53 · S3 이미지 저장소(PR #33) · ForwardedHeaders + IP RateLimiter(PR #34) · `/health/live`·`ready`, `UseHttpsRedirection` 제거, 보안 헤더·강제 CSP(PR #35)
- 오늘 배운 것 3줄 (내 질문 기준, Claude 작성 — 사용자 요청):
  1. 도메인 이름은 산 뒤에 바꿀 수 없다 — 새로 사서 옮겨야 하고, 그때 SES 인증·인증서·`NOOK_PUBLIC_ORIGIN`·카카오 등록·쿠키 도메인을 다시 한다. 바꾸는 비용은 사용자가 생긴 뒤(북마크·받은 메일 링크)가 가장 크다
  2. 충돌 난 PR의 병합 버튼은 꺼지는 게 정상이고, 내용이 이미 `main`에 있으면 해결하지 않고 닫는다. 브랜치를 쌓아 PR을 나누면(base = 앞 브랜치) 리뷰 범위가 맞고, 병합은 반드시 앞 PR부터 — squash 대신 병합 커밋
  3. compose `stop`은 멈춤, `down`은 컨테이너·네트워크 삭제(볼륨은 남음 — Docker Desktop 목록에서만 사라짐), `down -v`는 DB 볼륨까지 삭제. 운영에서는 `-v`를 쓰지 않는다
- 막힌 것: (사용자 작성)
- 내가 결정한 것: Route 53 · 도메인 이름 PM 확인 후로 보류, 도메인 무관 코드 먼저 · `IImageStorage` · API 프록시 유지 · `adobe/s3mock` · 요청 제한은 C# API에서, 전체 + 인증 엄격(300/분, 5/10분) · 헬스 live/ready 분리, 배포 스크립트의 curl 컨테이너가 호출
- Claude가 대신 작성한 부분: 위 코드·테스트 전부(서비스·테스트 위임 기간), dep-plan·progress 갱신, 이 기록 형식
- 사실·교훈: 처음부터 초록인 테스트는 변이로 확인 — S3에서 형식 검사 테스트가 검사 없이도 통과하던 것 발견 · `ForwardedHeadersOptions` 신뢰 목록이 비면 "모두 믿음"(변이 실측) · 프록시가 2단(Caddy → BFF → api)이면 고리마다 신뢰 근거가 필요 · compose 네트워크 설정 변경 뒤 기존 컨테이너는 DNS 별칭이 빠짐 → `down`/`up` · CSP는 Report-Only로 측정 후 강제, 스킴 없는 출처 = 페이지 스킴 따라감 · 추정(daumcdn)은 측정(kakaocdn)으로 고침
- 면접 대비로 다시 볼 만한 것(Claude 제안): X-Forwarded-For 위조와 신뢰 프록시, 고정 창 vs 슬라이딩 창 vs 토큰 버킷, liveness vs readiness(재시작 판단에 의존성 검사를 넣으면 생기는 일), CSP `unsafe-inline`/`unsafe-eval`/nonce, HSTS 프리로드, 최소 권한(egress·ListBucket)
- 다음에 할 것: 도메인 이름 확정(PM) → 구매 → 5단계 인프라

## 2026-11-09 밤 (DEP — 도메인·SES·5단계 준비)

> 형식과 사실 항목은 Claude가 잡음. "배운 것"·"막힌 것"은 사용자 작성.

- 한 일: 도메인 `fillyournookwithstars.com` 구매 · SES 도메인 인증·DMARC `quarantine`·운영 승인 요청 · 카카오 도메인 · 읽기 역할 축소 · 시드니 SCP 차단 · S3 버킷 2개 · IAM 역할 `nook-ec2`
- 오늘 배운 것 3줄: (사용자 작성)
  1.
  2.
  3.
- 막힌 것: (사용자 작성)
- 내가 결정한 것: 도메인 이름(PM) · DMARC 지금 올리기 · 계정 리전 네임스페이스 · 백업 버킷 버전 관리 · 백업 정책 Put만·`daily/`·`weekly/` · `AbortMultipartUpload` 안 줌 · EC2는 6단계 뒤
- Claude가 대신 작성한 부분: IAM 정책 틀(백업 문 제외), 문서 갱신, 이 기록 형식
- 사실·교훈: SES는 리전마다 따로(시드니 사고) → SCP로 구조적 차단, 거부 오류로 검증 · DMARC는 SPF·DKIM 중 하나만 정렬되면 통과 · `p=none`은 관찰이라 테스트 전 게시해도 무해, 올리는 건 측정 뒤 · IAM에 없는 액션(`s3:PostObject`)은 조용히 아무것도 안 줌 · 정책 복사 후 일부만 고치면 접두사가 따라옴 · 자리 표시자는 개수 대신 "남은 것 0"으로 검사 · 콘솔 "성공"과 바깥 측정(DNS·메일 헤더)은 다르다
- 면접 대비로 다시 볼 만한 것(Claude 제안): SPF·DKIM·DMARC 정렬, SES 샌드박스·평판(반송 5%·불만 0.1%), SCP vs IAM(교집합·명시적 Deny), 신뢰 정책 vs 권한 정책·`iam:PassRole`, S3 버전 관리·삭제 마커·수명 주기, 버킷 이름 재등록 공격, 쓰기 전용 백업(랜섬웨어)
- 다음에 할 것: SES 승인 확인 → 6단계 비밀값 → EC2

## 2026-11-09 오후 (DEP-7·8·9) — BFF 인증 경로, 백업·복원, ARM 빌드, 메일 상한·SES

> 형식과 사실 항목은 Claude가 잡음. "배운 것"은 사용자 질문 기준으로 Claude 작성(사용자 요청 방식), "막힌 것"은 사용자 작성.

- 한 일: ADR-013 · BFF `/api/auth/*` Origin 검사·429 안내 · DB 백업·복원 리허설 · arm64 이미지·운영 compose·배포/롤백 스크립트(PR #37) · A1 메일 상한·SES 발송기(PR #38)
- 오늘 배운 것 3줄 (내 질문 기준, Claude 작성):
  1. 복원 리허설이 "덤프 뒤 쓰기"를 실패로 잡은 건 백업 문제가 아니라 비교 기준이 덤프와 다른 시점(지금의 운영 DB)이라서다 — 검사기가 살아 있다는 증명이면서, 쓰기 중인 운영에선 거짓 실패가 난다는 한계
  2. 느린 arm64 빌드(약 14분)는 PR이 아니라 배포 때만 한다 — 자주 하는 일(PR)과 드물게 하는 일(배포)을 나눠 비용을 옮겼다. PR CI images는 5분 39초(기준선)
  3. SES는 받은편지함이 없는 "보내기 전용" 서비스이고, 보내는 주소는 우리가 산 도메인(`no-reply@도메인`)이어야 한다 — DKIM 레코드로 소유를 증명해야 받는 서버(DMARC)가 믿는다. 그래서 도메인이 메일 발송의 전제
- 막힌 것: (사용자 작성)
- 내가 결정한 것: 백업 하루 1회·보존 7+4 · ARM은 교차 빌드 + web만 QEMU · PR CI는 x86 유지 · 리허설 스크립트는 수정 없이 진행 · 도메인·개인정보는 PM 확인 후
- Claude가 대신 작성한 부분: 코드·스크립트·테스트 전부, ADR-013, dep-plan·progress 갱신, 이 기록
- 사실·교훈: 로그인 CSRF는 쿠키가 없어도 성립 · exhaustive switch가 새 실패 이유의 문구 누락을 컴파일 오류로 잡음 · "아키텍처 무관한 결과물"과 "도구가 결과물을 실행해야 하는가"는 다른 질문(번들 교차 빌드 실패) · 처음부터 초록인 검사는 변이로 확인(A1 제거 → 12통) · SES 기본 문자 집합은 7비트 ASCII
- 면접 대비로 다시 볼 만한 것(Claude 제안): 로그인 CSRF와 Origin/SameSite의 차이, RPO·RTO와 "복원해 봤는가", 교차 컴파일 vs 에뮬레이션, 롤백과 확장 → 전환 → 축소 마이그레이션, 메일 인증(SPF·DKIM·DMARC)
- 다음에 할 것: PM 확인(도메인 이름·개인정보 정책) → 도메인 구매 → 5단계 인프라
