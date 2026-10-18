# 진행 내역

- 시작일: 2026-10-08
- 현재: **D4 진행 중** (2026-10-18, D4-1). 브랜치 `feat/d4-tests`. 달력 기준 약 4일 지연
- 다음 할 일 (순서대로)
  0. ~~D4-1 커밋~~ ✅ `f920a05` + SQL 로깅 커밋
     - 통합 테스트 실행 조건: Docker Desktop(엔진)만 켜져 있으면 됨. compose `db`는 불필요
     - ~~미답 질문: EF 맞바꾸기 UPDATE 순서~~ ✅ SQL 로그로 확인 — 기본 키 순, 같은 밀리초 UUIDv7은 대소 무작위라 실행마다 바뀜. 어느 순서든 중간에 중복 → DEFERRED 필요. 로그: `Db.CreateContext(output.WriteLine)` + `--logger "console;verbosity=detailed"`
  1. ~~PR #5 병합, D4 브랜치~~ ✅ / ~~Testcontainers 기반~~ ✅ `PostgresFixture`(postgres:17, `MigrateAsync`, `ResetAsync`=TRUNCATE CASCADE) + `IntegrationTestBase`
  2. 예시 테스트 `GroupPersistenceTests.Create_WhenSaved_ReloadsCreatorAsOnlyMember` ✅(Claude 작성 — 사용자가 이 형식을 따라 나머지 작성)
  3. D3에서 넘어온 통합 테스트 목록
     - ~~그룹 생성 커밋 후 멤버 1명~~ ✅
     - ~~리뷰 이미지 순서 맞바꾸기(0↔1)~~ ✅ `ReviewPersistenceTests`(Claude 작성). 도메인 공개 메서드 없음 → 변경 추적기로 `SortOrder` 변경, RV-02 메서드 생기면(D5) 도메인 경로로 바꾸기
     - 라운드트립은 저장·검증 `DbContext`를 분리(식별 맵)
     - ~~엔티티마다 `Create(..., default)` 예외(ADR-007)~~ ✅ Bookmark·Group(Create·AddMember)·Like·Review, default+비UTC, `ParamName`까지. 변이(가드 제거·순서 이동)로 빨간불 확인. **D4부터 테스트 코드는 대부분 Claude 작성**(사용자 요청, 사용자는 예측·결과 확인)
  4. ~~erd.md 갱신~~ ✅ (Claude 작성, 사용자 요청): review_images "확인 항목" 결과(EF 모델에 알리지 않음, 맞바꾸기·위반·`EXPLAIN` 실측), "예상" 표기 중 실측한 2건(users `lower()`, review_images Q1·Q3) 표기 변경
  - 이월: `varchar(56)`→`varchar(60)` 확장 시 `pg_relation_filenode` 전후 비교(미실행), 나머지 "예상" `EXPLAIN`은 데이터가 있어야 의미 있음 → D8 시드와 함께, 시드 스크립트 위치도 D8
  - 선택: `Review` 수정(RV-02, 기존 `ReviewImageId`와 새 키가 섞인 목록) — D5로
  - ~~미답: Configuration 주석~~ ✅ A안 — PK·UNIQUE·인덱스마다 `// 받는 쿼리: erd.md <table> Q..` (Q 번호만, 근거는 erd.md). 마이그레이션 SQL 제약 2곳도 동일. 인덱스·쿼리를 바꾸면 erd.md 표와 함께 고친다
  - 미답: `.editorconfig`에 namespace 분석기(`IDE0161`) 추가 여부 — namespace 사고 3회(D2-1·D2-2·D2-4)
  - 정리: 템플릿 `UnitTest1.cs` — Integration은 삭제 ✅, Domain·Algorithms 남음(D4)

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

커밋: `6b0d250` 골격 · `40e1f97` 패키지 보안 업데이트 · `a92da37` 프로젝트·참조 (모두 `feat/d1-skeleton`, PR #2로 머지)
Claude가 작성: `.gitattributes`, `dotnet new`/참조 명령어

### 2026-10-08 (D1-2)
1. 브랜치 `feat/d1-usecases` 생성, `.gitignore`에 `cs_trainning/`·`CLAUDE.local.md` 제외 추가
2. 유스케이스 작성 요령 학습: 시스템 경계, 기본/실패 흐름(갈라지는 단계 번호), 사후 조건은 화면이 아닌 DB 상태, "실패 시 보장"
3. 원본 확인: 장소 검색은 프론트가 카카오 SDK로 직접 수행(`src/lib/kakao.ts`) → 백엔드 유스케이스는 "선택한 장소 확정"(PL-01)부터. 원본 `upsertPlace`(`src/lib/store.ts:125`)는 조회→INSERT 분리로 TOCTOU
4. PL-01 장소 확정(Claude 예시), BM-01 장소 저장(사용자 작성, 리뷰 3회) — 별 상태는 북마크·리뷰 조합으로 계산되는 값, 존재하지 않는 장소, 동시 요청 실패 흐름 포함
5. BM-02 빈 틀 추가(`TODO(human)`)

커밋: `9f10d27` .gitignore · `6ea1d40` PL-01·BM-01 (`feat/d1-usecases`, 미푸시)

### 2026-10-09 (D1-3)
1. BM-02 저장 해제 — 별 상태 비대칭, 실패 시 보장은 "요청 전과 비교해 변하지 않는 것"
2. 공통 규칙 절 도입(R-PL·R-BM·R-RV·R-GR·R-STAR) — 규칙은 한 곳, 유스케이스는 ID 참조
3. R-BM-2 저장·해제 경합: 나중에 처리된 요청이 최종 상태(초안 "둘 다 실패"는 서버가 동시를 감지할 수 없어 폐기)
4. RV-01 리뷰 작성 — 싼 검증 → 이미지 업로드 → DB 저장 순서, 고아 이미지는 "노출되지 않음 + 정리 배치"
5. GR-01 그룹 합류 — 미리 검사(3b·4b)와 최종 보장(5a·5b) 구분, 불변식 3줄(정원 ≤ 4, 참여 ≤ 3, 멤버십 0/1)
6. 나머지 유스케이스 17개 목록(이름·실패 흐름 키워드), 원본 명세에서 "리뷰 같은 장소 1일 1회" 발견
7. ADR-001 채택: Controllers, 리포지토리 인터페이스 Application, 전이 참조 차단 + Infrastructure→Domain 유지, 엔티티 겸용 + Fluent API + 지연 로딩 미사용
8. `Directory.Build.props`에 `DisableTransitiveProjectReferences` — Api에서 Domain 타입 사용 시 CS0234 확인
9. 검증: `dotnet build` 경고·오류 0, `dotnet test` 3/3 통과

커밋: `411fc6e` BM-02·공통 규칙 · `97f7926` RV-01 · `b6cda93` GR-01 · `b39b259` build props · `d06fdea` 유스케이스 목록 · `c455bec` ADR-001 · `190f71c` 학습 로그 (`feat/d1-usecases`, 미푸시)
Claude가 작성: PL-01 예시, 공통 규칙 재구성, R-BM-1·2, GR-01 불변식, 유스케이스 목록 17개, ADR-001 빈칸·결과 절, `Directory.Build.props`
Claude가 작성: PL-01(예시), BM-01·BM-02 빈 틀

### 2026-10-09~10 (D2-1)
1. 실패 표현 원칙: 예상 가능한 실패 → Result, 개발자 실수 → 예외. Domain은 HTTP를 모르고 Api가 `ErrorType`을 상태 코드로 번역
2. 오류의 두 축: 분류(`ErrorType` Validation·NotFound·Conflict·Forbidden, 숫자 없음) / 식별(`Error.Code` = `"애그리거트.규칙"`, 클라이언트 계약). `Error`는 `sealed record`, 빈 `Code`는 예외. 설명문은 개발자용, 화면 문구는 클라이언트 몫
3. NotFound는 Domain이 아니라 Application(리포지토리 null)이 발견. Unauthorized는 미들웨어, InternalError는 예외 → enum에서 제외
4. `Result<T>`: `Value` 비공개, 출구는 `TryGetValue` + `[NotNullWhen]`(가드 절로 평평한 호출 코드). `Success(null)`·`Failure(null)` 거부. scratchpad로 CS8604 경고 확인
5. 값 없는 결과는 비제네릭 `Result`(C안) — `IsFailure(out error)`, 성공 객체 `static readonly` 캐싱. A안(`Error?`)은 실패 표현이 두 가지가 되어 기각
6. VS 자동 using(`JSType.Error`)이 `Error.cs`를 오염시킨 사고 → 사용자가 복원
7. `ReviewContent`: trim → 검증 → trim된 값 보관. 원본 nook은 trim 전 길이라 `"좋"+공백9`가 통과하던 허점 → ADR-002 "원본과 다른 점" 후보
8. 글자 수 단위 = **코드 포인트**(`EnumerateRunes`) — PostgreSQL `char_length`와 일치해야 DB CHECK 이중화 가능. 측정: 😀 Length 2 / Rune 1, 👨‍👩‍👧 8/5/1(그래핌)
9. `struct` 값 객체는 `default`로 검증 우회 → `sealed record`. 참조 타입이면 미초기화 시 CS8618로 컴파일 시점에 잡힘(측정 확인)
10. 검증: `dotnet build` 경고·오류 0. 경계값(null·공백·9/10/50/51자·이모지 50/51)을 scratchpad 콘솔로 확인 — 단위 테스트는 D4

커밋: `e1f2c3e` Error·Result<T> · `aee7eb2` 비제네릭 Result · `4761604` ReviewContent(메시지 amend, 구 `071120c`) · `691f9d3` R-RV-01 문구 · `6aa7ab5` 진행 기록 (`feat/d2-domain`, 미푸시)
Claude가 작성: `Error.cs`·`Result.cs`·`ReviewContent.cs` 빈 틀, `ReviewErrors.cs` 전체와 `ReviewContent`의 오류 반환 줄(사용자 요청, 힌트 4단계), `ReviewContent` 리뷰 후 수정(사용자 요청: struct→sealed record, null 처리, `Value` 공개, 길이 1회 계산, 상수 PascalCase)

### 2026-10-10 (D2-2)
1. Git 정리: 오타 커밋 `--amend`, 이미 쌓인 커밋에 `--fixup` + `rebase --autosquash`(비대화형 `GIT_SEQUENCE_EDITOR=:`). 원칙: 푸시 전이면 다듬고, 푸시 후면 쌓는다
2. `Coordinate`: **불변식(물리 범위) vs 정책(서비스 지역)** 분리 — 값 객체·DB CHECK는 지구 범위, 한국 영역은 PL-01 1a(싼 검증, DB 조회 전). DB CHECK는 "규칙이 바뀌면 기존 데이터가 틀린 데이터가 되는가"로 판단 → 정책을 CHECK에 넣으면 정책 변경마다 마이그레이션(축소 시 기존 행 때문에 실패)
3. NaN은 거부 조건(`< min || > max`)을 통과 → **허용 조건**(`is not (>= and <=)`)으로 검증. 첫 구현의 `&&` 실수도 함께 사라짐. 측정: NaN·±∞ 거부
4. `struct` 조건 보강: 모든 비트 조합이 유효하고 **`default`가 의미 있는 값일 때만** — `(0,0)`은 유효하지만 미초기화와 구분 불가 → `sealed record`
5. `Nickname`: NFC 정규화(측정: "강" NFC 1 / NFD 3 코드 포인트, 12음절 NFD 24) → trim → 길이 → **화이트리스트**(한글 완성형·ASCII 영문·숫자). `char.IsLetterOrDigit`는 모든 문자 체계 허용이라 제외. U+3164(투명 닉네임)·ZWSP·전각·아랍 숫자 거부 확인. 짝 없는 서로게이트는 `Normalize` 예외 → Result로 변환
6. `InviteCode`: 생성(신뢰, 실패=예외)과 파싱(불신, 실패=Result) 두 경로. 생성은 `Func<int,int>` 주입(A안) → Domain leaf 유지, 테스트에서 `n => 0`. ASCII에만 `ToUpperInvariant`(Turkish I, `tr-TR`로 측정). 형식 오류는 공개 규칙이라 구분해도 정보 누출 없음 → 존재하지 않는 코드와 별도 `Code`
7. 사고: `GroupErrors`/`PlaceErrors` 파일 내용이 뒤바뀜 — C#은 폴더와 namespace 불일치를 잡지 않아 빌드 성공. `git status`의 예상 밖 `M`으로 발견
8. `.editorconfig` 추가(utf-8, lf, final newline) — VS의 BOM·CRLF로 생기던 `git add` 경고 제거. 작업 폴더 CRLF·mixed 파일 LF로 변환
9. 분류표: 애그리거트 = 함께 일관성을 지켜야 하는 묶음. Bookmark·Like는 쓰기 경합 때문에 독립 루트, ReviewImage·GroupMember는 내부 엔티티. **다른 인스턴스와 비교해야 하는 규칙(유일성)은 저장소가 최종 보장**. 규칙 ID ↔ 분류표 대조로 R-GR-06·07 누락 발견
10. 리뷰 수: B안(같은 장소 하루 1개, R-RV-03). 기준 UTC 00:00(KST 09:00), 작성일은 Domain이 `TimeProvider`로 결정
11. 검증: `dotnet build` 경고·오류 0, `dotnet test` 3/3 통과. 값 객체 경계값은 scratchpad 콘솔로 확인(좌표 12, 닉네임 25, 초대코드 27) — 단위 테스트는 D4

커밋: `660b44b`·`914ce8f` editorconfig · `9ae3875` Coordinate · `58533d5` Nickname · `6aa62d3` InviteCode · `38191aa` 규칙 추가·ID 통일 · `481bf46` 분류표 (`feat/d2-domain`, 미푸시)
Claude가 작성: `Coordinate`·`Nickname`·`InviteCode` 빈 틀, `.editorconfig`, BOM·CRLF·final newline 일괄 정리, `domain.md` 분류표 틀, `usecases.md` 규칙 R-US-01·02·R-LK-01·R-GR-06·07 문구(사용자 요청, 내용은 사용자 결정)와 R-BM 참조 번호 변경, R-RV-03 초안(사용자가 반영), 분류표 🟡 수정(사용자 요청: R-US-02→User, R-GR-06 추가, Like 표기, R-GR-07 근거 보강)

### 2026-10-11 (D2-3)
1. 애그리거트 경계 = 한 트랜잭션에서 함께 바뀌고 일관성이 보장되는 묶음. 경계 밖은 ID로만 참조 — Review가 Place 객체를 들면 Place 규칙(R-PL-02)을 우회하는 두 번째 변경 경로가 생기고, 로딩·추적·잠금 범위가 커진다
2. 클래스 다이어그램(Mermaid): 애그리거트별 `namespace`, 경계 안 `*--`, 경계 밖 ID 속성 + `..>`. 다중성 숫자 = 불변식 선언이므로 분류표 "지키는 불변식" 열과 대조
3. 생성 불변식(팩터리 한 곳) vs 생애 불변식(상태를 바꾸는 모든 메서드). 하한 `1..4`가 빈 그룹 배치 정리안과 충돌 → 정책 번복: 만든 회원이 첫 멤버(R-GR-08), 마지막 멤버 탈퇴 시 같은 트랜잭션에서 그룹 삭제(R-GR-09). 엔티티는 자기 삭제를 못 하므로 판정은 루트, 삭제는 Application. 불변식은 트랜잭션 경계에서 성립하면 된다
4. 배치(최종적 일관성)를 쓰는 기준: 외부 I/O처럼 트랜잭션에 못 넣는 경우(이미지). 같은 애그리거트 DB 작업을 배치로 미루면 중간 상태(빈 그룹 + 살아 있는 초대 코드)와 합류·정리 경합을 떠안는다
5. 외래 키 오해 정정: 제거(`DROP CONSTRAINT`)는 메타데이터 변경으로 싸고, 나중에 추가하는 쪽이 기존 행 검증으로 비싸다 → FK 유지. 대리 키 수와 FK 사용은 무관 — 복합 PK여도 FK는 건다
6. 대리 키 정리: Bookmark·Like·GroupMember는 복합 키(유일성 규칙이 PK 자체가 됨, 인덱스 1개). ReviewImage는 순서·교체 때문에 식별자 유지. 경계 안 엔티티(GroupMember)에는 부모 ID를 두지 않음 — DB 매핑(D3) 관심사
7. 엔티티 자식 컬렉션 ≠ 값 객체 컬렉션: 루트는 바꿀 수 있어야 하고 EF Core가 채워야 함 → backing field. `ImmutableArray` 매핑 여부는 D3에서 확인
8. Write Skew: 남은 두 멤버가 동시 탈퇴 → 서로 다른 행을 지워 행 잠금 충돌 없이 둘 다 "1명 남음"으로 판단 → 빈 그룹. 원본 nook의 "정원 초과"와 "고아 그룹"은 같은 원인. 해법은 공통 행(그룹 행)을 **인원을 세기 전에** 잠그기. Read Committed는 문장마다 새 스냅샷이라 대기 후 다음 SELECT가 커밋을 본다
9. 그룹 생성도 R-GR-02 검사·R-GR-04 사용자 행 잠금 대상(2개 참여 중 동시 생성)

커밋: `0789312` 클래스 다이어그램·R-GR-08·09·분류표 (`feat/d2-domain`, 미푸시)
Claude가 작성: 다이어그램 빈 블록·`TODO(human)` 틀, R-GR-08·09 문구와 R-GR-09 동시성 근거(사용자 요청, 내용은 사용자 결정), `progress.md` 갱신

### 2026-10-12 (D2-4)
1. Git: `--amend`로 커밋 메시지 오타·줄바꿈 수정 → 해시가 바뀌므로 문서의 해시 참조도 갱신
2. 강타입 ID: `record class`(struct는 `default`가 `Guid.Empty`로 조용히 통과). `New`(v7 발급)/`From`(복원, Empty는 예외). `From(Guid.Empty)`가 클라이언트 경로 값이면 500이 되는 문제 → D5 미결
3. 공통화 측정(scratchpad): 비제네릭 공통 기반만 `userId == placeId`가 컴파일(항상 False), 제네릭 자기 타입 기반·인터페이스는 CS0019. `.Equals(object)`는 모두 컴파일 → C안(인터페이스 `IEntityId<TSelf>` + 최소 복사). 인터페이스 목적 = D3 제네릭 값 변환기(`static abstract From`)
4. 엔티티 기준(Bookmark에서 확정): 동등성 재정의 안 함, 실패할 규칙이 없으면 `Result` 없이 반환, 시각은 `DateTimeOffset` 인자(`TimeProvider` 기각 — 도메인 순수성·한 유스케이스에서 같은 시각). `Review`도 통일
5. `TimeGuard.Utc()`: `default`·비UTC는 호출 경로 이탈(`DateTimeOffset.Now`) → 예외. `[CallerArgumentExpression]`으로 원래 인자 이름 유지(측정). 값 객체 대신 가드 — 시각이 들어오는 길은 `Create` 한 곳
6. 정합성 장치의 범위: 엔티티 메서드(인스턴스 1개) < EF 식별 맵(DbContext 1개 = 요청 1개) < DB 제약·잠금(모든 요청). `Group`의 `.Any()`는 미리 검사일 뿐
7. `ReviewImage`: `internal`(어셈블리 단위 — Application 차단, Domain 안은 열림). 순서 변경 = 같은 Id, 교체 = 새 Id(RV-02 "교체된 이미지가 남음" 방지). `sortOrder`는 `Review`가 목록 인덱스로 → 중복·빈틈 구조적으로 불가능. 스토리지 키 저장(URL 기각)
8. `Review`: `AsReadOnly()`로 캐스팅 우회 차단, `ReviewDate` 속성 → `review_date` 컬럼 UNIQUE(표현식 인덱스 기각), "예외 먼저 → Result 나중", `Review.MaxImages`(타입별 const로 SSoT 결정)
9. `Place`: 입력은 카카오 응답이 아니라 **클라이언트 JSON**. 형식은 느슨하게(원본 seed `demo-1`), 크기 상한은 항상(`KakaoPlaceId` 64자 — UNIQUE 인덱스 보호). 형식 검증 ≠ 크기 검증
10. `User`: `ChangeNickname`은 표기까지 같으면 멱등, 대소문자만 바꾸는 건 허용(R-US-02 보강). `Email` + R-US-03(느슨한 형식, 대소문자 무시 유일). 응답 DTO는 Api 몫 → `void`
11. 사고: `Reviews/UserId.cs`(내용은 ReviewId), 복사한 주석 잔존, 자동 `using`, **`ReviewErrors`의 namespace 줄 삭제(전역 namespace로 이동, 빌드 통과)** — C# 컴파일러가 잡지 않는 종류. diff 확인으로 발견
12. 검증: `dotnet build` 경고·오류 0, `dotnet test` 3/3 통과. `KakaoPlaceId` 경계값(null·공백·64/65자·이모지 64/65)은 scratchpad로 확인 — 단위 테스트는 D4

커밋: `0789312`(구 `fd228c7` amend) · `1e64eff` D2-3 기록 · `3e0523a` UserId·PlaceId · `5fd4d92` Bookmark · `f3d0cd0` 시각 인자 통일 · `c67f266` IEntityId·ReviewId · `07d8a13` · `732a945` TimeGuard · `9121cd6` Like · `4b5fbfa` · `20b9534` 스토리지 키 · `71dc63e` ReviewImageId · `f12eed9` ReviewImage · `3d81c42` · `62a4a58` Review · `936b66c` · `4464585` Place·KakaoPlaceId · `f05d0eb` User·Email · `f6ec9ca` (`feat/d2-domain`, 미푸시)
Claude가 작성: `PlaceId`·`ReviewImageId`(UserId·ReviewId 복사, 사용자 요청), 엔티티 빈 틀·`TODO(human)`, `KakaoPlaceId` 64자 상한 검증(사용자 요청, 상한값은 사용자 결정), `Review.cs` 키 검사 순서 주석(사용자 요청, 결정은 사용자), `domain.md`·`usecases.md` 스토리지 키 반영, R-RV-03 근거 문구 수정, `progress.md` 결정 기록

### 2026-10-13 (D2-5)
1. Group 설계 답변: 그룹명 R-GR-10, `AddMember(UserId, joinedAt)`, 검사 순서 중복 → 정원, `RemoveMember` 멱등 + `MemberRemoval` 3상태
2. `GroupName`(값 객체, Nickname 복사 — 바뀌는 이유가 달라 공유 안 함), `GroupMember`(`internal`, 자연 키 UserId)
3. **Group을 TDD로**(첫 TDD): 테스트 목록 먼저 → Red → Green. C#의 Red는 두 단계 — 없는 메서드는 컴파일 오류(올바른 실패 아님) → 시그니처 스텁(`NotImplementedException`) → 단언 실패
4. TDD 실수와 교정: `Create`는 구현을 먼저 써서 Red를 못 봄 → **손으로 하는 뮤테이션 테스트**(`_members.Add` 주석 처리 → 59번 줄 `Expected 2 / Actual 1`)로 테스트 가치를 사후 증명. 경계·순서 테스트는 검사 순서를 뒤집어 실패 확인. 리뷰 중 나온 결정(빈 그룹 가드)이 테스트 없이 코드로 들어감 → 테스트 추가 후 Red → Green
5. 첫 스텁의 `_ = Members.Append(...)` — LINQ `Append`는 새 시퀀스를 반환하고 원본을 안 바꿈. `_ =`가 경고까지 지움
6. 생성자는 "데이터 복원", 행동(첫 멤버 추가)은 `Create` — EF Core가 속성과 맞는 생성자로 물질화하므로 생성자에 행동을 넣으면 로드할 때마다 실행
7. `Create`는 `Group` 반환(회원 입력 실패 없음 — GroupName이 이미 검증), `MaxGroupMembers = 4`(첫 스텁의 3은 R-GR-02 값과 혼동)
8. 방장 없음(ADR-004 결정 6): 읽는 유스케이스가 없음. 원본 `owner_id ... on delete cascade`는 방장 회원 탈퇴 시 남은 멤버의 그룹까지 삭제
9. `MemberRemoval.NotMember = 0`: enum 기본값이 "아무 일도 없음"이라 미초기화 값이 그룹을 지우지 않음. `GroupEmptied`는 방금 0명이 됐을 때만 → 삭제 신호 중복 없음. 빈 그룹 `AddMember`는 `InvalidOperationException`(호출 코드 실수)
10. 오류 `AlreadyMember`·`GroupFull`은 `Conflict`(입력이 아니라 서버 상태와 충돌 → D5 409). enum은 `Groups/` 바로 아래(package by feature, 폴더 = namespace)
11. design-review(src/Nook.Domain): 🔴 1 — `Group.Create`가 null `GroupName`·`InviteCode`를 받아 그룹을 만듦(테스트로 Red 확인 후 수정). 🟡는 아래 미결 결정으로
12. 검증: `dotnet build` 경고·오류 0, `dotnet test` Domain 21 · Algorithms 1 · Integration 1 통과

커밋: `4913ea5` GroupMember · `a35b6bc` Group.Create · `705f38a` AddMember · `c941cd8` RemoveMember · `f0b3bd6` Group null 검사 (`feat/d2-domain`)
Claude가 작성: `GroupTests` 빈 틀·`TODO(human)`, ADR-004 결정 5·6 추가(사용자 요청, 결정은 사용자), design-review 🔴 수정(`Group.Create` null 테스트 2개·null 검사·헤더 주석 — 사용자 요청), `progress.md` 갱신

### 2026-10-15 (D3-1)
1. 복합 인덱스 leftmost prefix: `group_members` PK `(group_id, user_id)`는 `WHERE user_id = ?`를 범위로 좁히지 못함. PK도 인덱스라 같은 순서 인덱스를 또 만들면 중복 → PK `(group_id, user_id)` + 보조 `(user_id)`. 정원 검사는 Group 로딩 후 메모리 Count라 `COUNT(*)` 쿼리 없음
2. `erd.md` 형식: 테이블마다 "읽는 쿼리(Q1~)" 표를 먼저 쓰고, 제약·인덱스마다 "받는 쿼리 / 막는 것"을 적는다
3. users·places: `lower()` 표현식 UNIQUE(쿼리 모양을 `lower(col) = lower(?)`로 맞춰야 함), 닉네임·이메일·장소 CHECK 이중화. `btrim`은 ASCII 공백만 지움 → users·places는 최소 이중화, groups는 허용 공백이 ASCII뿐이라 완전 이중화
4. design-review(users·places): 🔴 1 — `ux_places_kakao_place_id`가 `users`를 가리킴(수정). 🟡 PL-01 동시 등록 쿼리 누락(Q3 `ON CONFLICT` 추가), `kakao_place_id` CHECK 누락, 측정 안 한 EXPLAIN 결과 → "예상"으로 표기, 측정값 제거
5. 행 잠금: FK 검사는 부모 행에 `FOR KEY SHARE` → `FOR UPDATE`와 충돌, `FOR NO KEY UPDATE`와는 충돌 안 함(자기 자신과는 충돌 → R-GR-04 직렬화 유지). `lock_timeout` 기본 0(무한 대기)
6. R-BM-02 기준을 커밋 순서로 정함(A안): 네트워크에서 순서 역전 가능, 서버는 클릭 순서를 모름 → 클릭 순서는 클라이언트 책임. `usecases.md` 규칙·근거 수정
7. bookmarks·reviews·review_images·likes·groups·group_members 작성. `ON DELETE` 기준 2개: ① 정상 삭제 경로가 있는가(없으면 RESTRICT — places) ② 지워도 깨지는 불변식이 있는가(있으면 RESTRICT + 앱 절차 — `group_members.user_id`)
8. 결정을 미룰지는 되돌리는 비용으로: 인덱스는 나중에 `CONCURRENTLY`로 싸게 추가(BM-03·마이페이지 정렬 인덱스 보류), FK는 지금

커밋: (이 세션 마무리 커밋)
Claude가 작성: `erd.md` 골격(형식만), places `ck_places_kakao_place_id` 행·`ck_places_name` 근거, users 메모 2줄(`btrim` 범위, `lower()` 일치 미확인), §3 `lower()`·북마크 여부 행, review_images "확인 항목", groups "FK (들어오는 것)" 행 — 모두 사용자 요청, 결정은 사용자. `progress.md` 갱신

### 2026-10-16 (D3-2)
1. 별 상태: 저장 안 함, 조회 시 계산. 비용은 log N(전체) + k(그 회원의 행) — `user_id` 앞 컬럼 인덱스. 반대 순서 `(place_id, user_id)`면 범위를 못 좁혀 N에 비례. 저장하면 두 애그리거트가 같은 파생 행을 갱신 → 전이 계산은 lost update(RC에서 대기한 UPDATE는 SET 값을 재계산하지 않음), 원천 재계산은 write skew. 전환 비용은 계산→저장이 비싸지만(백필·이중 쓰기) 저장의 동기화 비용은 매일 냄
2. `ORDER BY` 없는 SELECT는 순서 보장 없음. UPDATE는 새 행 버전을 쓰므로(MVCC) 물리적 순서도 바뀜
3. `storage_key` 규칙: `reviews/{yyyy}/{MM}/{dd}/{32자 hex v4}.{jpg|png|webp}` → 최대 56자, `varchar(56)` + Domain `ReviewImage.StorageKeyMaxLength`(초과는 서버 실수 → 예외). 리뷰·이미지·회원 ID 미포함, 파일명 불신. 길이는 `Length`(≥ 코드 포인트 → DB보다 같거나 엄격). TDD로 55·56·57자 테스트(`ParamName` 단언 — 같은 예외 타입이 두 곳에서 나옴)
4. prefix도 정렬 키: 배치의 질문("최근 생긴 것")과 맞는 시간 prefix. prefix는 범위만 좁히고 나이 판정은 객체 Last-Modified로(날짜 경계 23:59:59 업로드 → 배치 → 커밋 순서면 정상 이미지 삭제)
5. 고아 이미지: 미참조 + 나이 > N(6시간). N 하한 = 서버 타임아웃(취소 토큰이 DB까지 전달될 때만 상한) + 여유. 클라이언트 타임아웃은 서버 작업을 멈추지 않음. 손해 비대칭(작으면 데이터 손실, 크면 비용) → 넉넉하게. 매일 06:00 최근 30시간(주기 + N) + 주 1회 전체 스캔(B안). Q4는 `= ANY(@keys)` 묶음 조회
6. transactional outbox(C안) 보류: 전환 비용 작음(백필 없음 — B가 과거 고아 담당). 전제: 삭제 흐름("키 읽기 → DB 삭제 → 커밋 → 스토리지 삭제") 한 곳(D5). 도입 조건: 주 1회 전체 스캔이 너무 오래 걸릴 때
7. `review_images` UNIQUE `(review_id, storage_key)` → `(storage_key)`: 다른 리뷰끼리 키 공유 시 즉시 삭제가 남의 이미지를 지움. Q1·Q3은 `(review_id, sort_order)` 앞 컬럼이 받는다고 예상
8. 자식 컬렉션: 공개 타입은 의미대로(`Images` = `IReadOnlyList` 순서 있음, `Members` = `IReadOnlyCollection`) — D2-5 🟡 "불일치"는 의도된 차이로 종료. 순서 보장은 Review가(EF 복원 순서 비보장), getter에서 매번 `SortOrder` 정렬. 검증은 D4 통합 테스트

커밋: (이 세션 마무리 커밋)
Claude가 작성: erd.md §3.1 정리(사용자 내용 이전 + 🔴·🟡 반영), review_images 컬럼·UNIQUE·Q4·메모 갱신, §3 고아 이미지 행 선택/이유 분리와 N·범위 반영, C 도입 조건 문구, §3 별 상태 행 선택·이유(사용자 요청), `ReviewImage.cs`·`Review.cs` 주석 정리(`Images` 주석 포함). Claude가 결정(사용자 위임): 길이 검사 단위 `Length`. `progress.md` 갱신

### 2026-10-17 (D3-3)
1. erd.md §4 원본 `schema.sql` 비교 → ADR-002 채택. 일정 변경안(포트폴리오 우선)은 보류, D5 종료 후 재산정(`docs/schedule-option-portfolio-first.md`)
2. ERD §1 Mermaid: 기호 두 축 — `||`/`|o`는 존재 의존(FK NULL 여부), `--`/`..`는 식별 의존(PK에 FK 포함 여부) → reviews·review_images 관계 3개를 점선으로. **라벨에는 DB가 보장하는 범위만**(리뷰당 0~3장), 앱·잠금이 보장하는 범위(그룹 1~4명, 회원당 3개)는 보장 주체 표로 분리
3. FK는 자식→부모 방향만 검사 → "자식 최소 1개"(그룹 멤버 ≥1)는 선언적 제약으로 표현 불가. `CONSTRAINT TRIGGER` 기각(규칙 중복, 숨은 로직·늦은 실패), 다이어그램은 `o{`, 앱 절차(ADR-004) + D4 테스트로 보장(erd.md §3)
4. 로컬 PostgreSQL 17.10 compose(`127.0.0.1` 바인딩, `${POSTGRES_PASSWORD:?}`로 비밀번호 필수). PG16+는 `lc_ctype`이 설정 변수가 아니라 DB 속성(`pg_database.datctype` = `en_US.utf8`, libc). `lower('ÄÖ')` = C#과 일치, 로케일 의존 문자는 미확인
5. EF Core 배치: 마이그레이션·Configuration은 Infrastructure(`--project`), 실행 환경은 Api(`--startup-project`). 마이그레이션이 Api에 있으면 DB 테스트가 HTTP 계층을 참조하게 됨. 대안 중 `EnsureCreated`는 SQL 제약이 빠져 이 프로젝트에서 치명적. 버전: Npgsql 공급자만으로는 EF Core 10.0.4 → `Relational` 10.0.13 명시로 Design과 맞춤(CPM은 나중에)
6. 생성(`Create`→Result)과 복원(`From`→예외, 빈 값만 검사, 값 그대로)을 분리 — 저장된 값은 CHECK를 믿어 규칙이 바뀌어도 읽힌다. 식 트리 제약 2개: `out` 변수, 정적 추상 멤버(CS8927 → 일반 정적 메서드로 감쌈). 실행되지 않는 가드(`ThrowIfNull` — NOT NULL·EF null 처리로 EF 경로에서 도달 불가)는 안전 착각만 준다
7. UNIQUE 제약 vs UNIQUE 인덱스: 제약은 DEFERRABLE O·식 X, 인덱스는 식 O·DEFERRABLE X → 둘 다 `migrationBuilder.Sql`(모델 스냅샷에 없음). FK 자동 인덱스 규칙을 꺼 erd.md 인덱스만 생성. 복합 프로퍼티는 생성자 바인딩 불가 → `Place` EF 전용 생성자(ADR-008)
8. 라운드트립은 다른 `DbContext`로(같은 컨텍스트는 식별 맵이 같은 인스턴스 반환 — 실측). 확인: 강타입 ID·값 객체·`Coordinate`·`Images` 순서·`Members` 복원, `RemoveMember` 후 행 DELETE, RESTRICT가 멤버십 남은 회원 삭제 거부, DEFERRABLE 맞바꾸기 COMMIT 성공·잔존 중복 COMMIT 위반, `EXPLAIN`: `lower()` 쿼리만 표현식 인덱스 사용, `WHERE review_id = ?`는 DEFERRABLE 제약 인덱스 사용(예상 일치)

결정: Configuration 하나씩 등록, 이름 명시(`EFCore.NamingConventions` 기각), 마이그레이션 A안(users 먼저 → `Init` 하나로 합침), DEFERRABLE은 EF 모델 밖, `ImmutableArray` 확인 항목 닫음(D3-2에서 `List` backing field로 정함), `From` 호출 제한은 D5(미결 결정)

커밋: `e7c1dab`(ERD 다이어그램) · `56b2e39`(compose) · `7655ce4`(EF 패키지) · `76ca3be`(erd 메모) · `7c4d261`·`14d919a`(`From`) · `a2a849b`(user-secrets) · `aa18e6a`·`97d77ab`·`cc62add`(매핑·마이그레이션) · 이 세션 마무리 커밋(ADR-008·progress)
Claude가 작성: erd.md §3 그룹 최소 1명 행, compose·`.env.example`, 패키지·로컬 도구·user-secrets, `From` 6개(Nickname·Email 구현은 사용자, 테스트·주석은 Claude)와 테스트, `EntityIdConverter`·`NookDbContext`·Configuration 6개·`Place` EF 생성자·`Init` SQL·`AddInfrastructure`, 라운드트립 실험 설계·실행, ADR-008, `progress.md` 갱신 — 모두 사용자 요청(작성 위임), 결정은 사용자

## 체크리스트

### 1주차 — 설계와 정합성
- [x] D1 도메인 분석·유스케이스·솔루션 골격·ADR-001
- [x] D2 클래스·엔티티·값 객체 (design-review 🔴 0) — Error·Result, 값 객체 8개, 강타입 ID 5개, 분류표, 다이어그램, 엔티티 8개, Group 단위 테스트 21개, ADR-004~007
- [x] D3 ERD·스키마·EF Core 매핑·마이그레이션·ADR-002 (design-review 🔴 0, `database update` 성공) — 테이블 8개, Mermaid·보장 주체 표, compose, `Init` 마이그레이션(SQL 제약 3개), `From` 6개, ADR-002·008
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
- **일정 변경안(포트폴리오 우선: D6 뒤 D12 배포 앞당김 + 프론트 연결) 보류 중** — 기존 순서로 진행하다가 결정. 근거·대화 내용은 `docs/schedule-option-portfolio-first.md`. 재산정 시점 D5 종료 후
- 복원용 `From`(`UserId`·`Nickname`·`Email` 등) 호출 제한 — `internal` + `InternalsVisibleTo`(컴파일 시점, Domain이 Infrastructure 어셈블리 이름을 앎) vs ArchUnitNET(메서드 호출 규칙, 계층 규칙과 묶을 때 가치) vs 이름·주석만. 막을 Application 코드가 생기는 D5에 결정(ADR-008 "결과")
- 중복 저장/미저장 해제 요청을 오류로 볼지 성공(멱등)으로 볼지 — D5
- 마지막 멤버의 탈퇴 두 번째 요청: 그룹이 이미 삭제돼 Application이 NotFound → 첫 요청은 성공, 두 번째는 실패라 R-GR-09 멱등 근거와 충돌. 204로 볼지 — D5
- 북마크 API: 토글 vs PUT/DELETE — D5. R-BM-02 A안(커밋 순서, 클릭 순서는 클라이언트 책임) → 멱등한 PUT/DELETE 쪽으로 기움. likes(LK-01)도 같은 구조
- 배포 대상(단일 VM vs Azure vs Fly.io) — D12 전까지
- 브랜치 접두사 `chore/*` 허용 시 CLAUDE.md 반영
- PR 머지 방식(merge / squash / rebase)
- 리뷰 본문 trim 범위: `string.Trim()`은 전각 공백(U+3000)은 지우지만 폭 없는 공백(U+200B)은 남김 — 허용할지 D4 테스트에서 결정 (닉네임은 화이트리스트로 해결됨)
- `ReviewContent`에도 NFC 정규화를 적용할지 — NFD 입력은 코드 포인트가 부풀어 50자 제한에 먼저 걸림. D4에서 재현 후 결정
- 이메일 소문자 변환 일치: PostgreSQL `lower()`(DB `LC_CTYPE` 따름) vs C# `ToLowerInvariant()` — 비ASCII에서 다를 수 있음. DB를 띄운 뒤 `SELECT lower('ÄÖ')`로 확인
- 회원 탈퇴 중 같은 회원의 다른 요청이 새 그룹에 합류하면 users DELETE가 RESTRICT 오류 → 탈퇴 트랜잭션도 처음에 users 행을 잠가야 하는가(R-GR-04와 연결) — D6
- D4 테스트 항목: 멤버가 남은 그룹은 Application 경로로 삭제되지 않는다(`group_members.group_id` CASCADE 유지의 대가), review_images 순서 맞바꾸기 저장(DEFERRABLE UNIQUE + EF 명령 순서), 순서 맞바꾼 리뷰를 다시 불러와 `Images[i].SortOrder == i`
- 정원이 찬 그룹에서 멤버가 나가면 노출된 초대 코드가 다시 유효해짐 — 코드 재발급·만료 필요 여부, GR 유스케이스 상세화 때
- 합류는 사용자 행(R-GR-04)과 그룹 행(R-GR-03)을 둘 다 잠근다 — 데드락을 피할 잠금 순서(예: 항상 사용자 → 그룹) — D6
- 고아 이미지 정리 배치 구현 시: 삭제 흐름을 한 곳에 모으기(C안 전환 전제) — D5. 전체 스캔 소요 시간 측정(C안 도입 기준선) — D8
- 강타입 ID `From(Guid.Empty)`는 예외 → 클라이언트가 빈 Guid를 경로로 보내면 500. Api/Application에서 미리 거를지(A) vs `From`이 허용하고 NotFound로 둘지(B) — D5
- ID Guid v7 vs v4: 삽입 성능·인덱스 크기 측정(주석의 "페이지 분할이 적다"는 미측정) — D8. v7은 생성 시각이 ID에 드러남
- ADR 번호: 003(마이그레이션·배포, D12)은 `개발_프로세스.md`에 예약됨 → 새 ADR은 008부터. 작성됨: 002 원본과 다른 점(Claude 작성, 2026-10-17 사용자 검토 후 채택), 004 그룹 생애, 005 FK 유지, 006 강타입 ID, 007 엔티티 시각(Claude 작성, 2026-10-13 사용자 검토 후 채택)
- ADR-002 범위 = 스키마 차이만(D3-2 사용자 결정, 근거 erd.md §4). 닉네임 UNIQUE는 포함. 도메인 규칙 차이(리뷰 길이 trim 후 계산, 하루 기준 UTC(원본 "00시"), 닉네임 화이트리스트·NFC)를 어디에 남길지 미정 — `domain.md`에 이미 있으면 정리 불필요
- design-review 🟡(D2-5, 고칠지 사용자 결정): `Group.MaxGroupMembers` vs `Review.MaxImages` 이름 규칙 불일치(`MaxMembers`?) · `GroupErrors`·`PlaceErrors`·`UserErrors` 헤더 주석 없음(`ReviewErrors`만 있음) · ~~`Members`/`Images` 공개 타입 불일치~~ → 의도된 차이로 종료(D3-2)
- `AddMember`의 `joinedAt`이 그룹 `CreatedAt`보다 이른 값이어도 통과 — Domain이 검사할지, Application 시각 경로(`GetUtcNow` 한 번)로 충분한지 — D4

## 확정된 결정 (D3)
- 테이블·컬럼·제약 이름은 snake_case (`pk_`·`ux_`·`ix_`·`ck_`·`fk_` 접두사)
- 대소문자 무시 UNIQUE는 `lower()` 표현식 인덱스(`citext` 기각: 확장 불필요, 비교 규칙이 쿼리에 드러남. 대가: 쿼리를 `lower(col) = lower(?)` 모양으로)
- 북마크·좋아요 여부 컬럼 없음 — 행 존재 = 상태
- `group_members` PK `(group_id, user_id)` + 보조 `(user_id)`, `likes` PK `(review_id, user_id)` + 보조 `(user_id)`, `bookmarks` PK `(user_id, place_id)`, 보조 없음
- `ON DELETE`: users 자식은 CASCADE, 단 `group_members.user_id`만 RESTRICT(R-GR-09 판정을 거쳐야 함). places 자식은 RESTRICT(삭제 경로 없음). reviews 자식(review_images·likes)은 CASCADE. `group_members.group_id`는 CASCADE 유지(애그리거트 경계 표현, 버그 경로는 앱 테스트)
- 이미지 최대 3장 DB 보장: `review_images (review_id, sort_order)` UNIQUE DEFERRABLE INITIALLY DEFERRED + `sort_order BETWEEN 0 AND 2`
- R-BM-02: 커밋 순서상 나중 요청이 최종 상태, 클릭 순서는 클라이언트 책임
- 측정 안 한 EXPLAIN 결과는 "예상"으로 표기
- 별 상태는 저장하지 않고 조회 시 계산(목록은 묶음 조회, N+1 금지). 재검토: D8 측정 → D9 카운터·D11 캐시 먼저
- `storage_key`: `reviews/{yyyy}/{MM}/{dd}/{32자 hex v4}.{jpg|png|webp}`, `varchar(56)`, Domain 상수·예외, 형식 CHECK 없음, `UNIQUE(storage_key)`
- 고아 이미지: 미참조 + 나이 > 6시간, 매일 06:00 최근 30시간 + 주 1회 전체(B안), 즉시 삭제는 커밋 후, outbox(C안)는 전체 스캔이 느려지면
- 자식 컬렉션: 공개 타입은 의미대로, 순서는 Review getter가 정렬해 보장

## 확정된 결정 (D2)
- 실패 = Result(예상 가능) / 예외(개발자 실수). Domain은 HTTP 모름
- `ErrorType` 0번 = `Validation` 유지(사용자 결정)
- `TreatWarningsAsErrors` 끔 — 패키지 업데이트 경고로 빌드가 깨지는 위험이 nullable 강제 이득보다 큼. D5(Api 코드 증가) 이후 재검토
- 값 없는 결과는 비제네릭 `Result`(C안)
- 텍스트 길이 단위 = 코드 포인트(닉네임·그룹명에도 적용), 검사·저장 모두 trim 후 값
- 불변식이 있는 값 객체는 `sealed record`(struct는 모든 비트 조합이 유효하고 `default`가 의미 있는 값일 때만)
- 값 객체 생성 순서: 정규화 → 허용 조건 검증 → 정규화된 값 보관. 오류는 첫 번째만 반환(필드별 `Code` 분리, 클라이언트 대응이 다를 때만 나눔)
- 좌표: 값 객체·DB CHECK는 지구 범위, 한국 영역은 Application 정책(PL-01 1a)
- 닉네임: NFC + 화이트리스트(한글 완성형·영문·숫자), 원래 표기 저장, 대소문자 무시 유일(R-US-01·02). 길이 검사가 글자 검사보다 먼저
- 초대 코드: 32자 집합 확정(I·O·0·1 제외, 원본과 일치), 생성은 `Func<int,int>` 주입, 소문자·앞뒤 공백은 정규화해 수용, 하이픈 거절
- Domain `*Errors` 카탈로그에는 Domain이 반환하는 오류만 둔다(`InviteCodeNotFound`는 D5 Application)
- 리뷰 수: 같은 장소 하루 1개(R-RV-03), 기준 UTC 00:00, 작성 시각은 Application이 `DateTimeOffset` 인자로 넘기고 UTC 날짜 계산은 Domain (2026-10-12 `TimeProvider`안에서 변경)
- 유일성 규칙(다른 인스턴스와 비교)은 저장소가 최종 보장. 애그리거트 안 비교(R-GR-07)는 루트가 미리 검사
- 규칙 ID 형식: `R-영역-두자리`
- 그룹 생애: 만든 회원이 첫 멤버(R-GR-08), 마지막 멤버 탈퇴 시 같은 트랜잭션에서 그룹 삭제(R-GR-09) — 빈 그룹 배치 정리안은 합류·정리 경합 때문에 기각. 다중성 `1..4`
- 그룹 생성도 R-GR-02 검사·R-GR-04 사용자 행 잠금을 거친다 (그룹 생성 유스케이스 상세화 때 반영)
- 탈퇴도 인원을 세기 전에 그룹 행을 잠근다(Write Skew 방지) — D6 재현 대상
- 외래 키는 유지한다. 제거는 메타데이터 변경으로 싸고, 나중에 추가하는 쪽이 기존 행 검증으로 비싸다
- 대리 키 제거: Bookmark·Like·GroupMember는 복합 키(D3에서 확정). 시간은 `DateTimeOffset`, 자식 컬렉션은 읽기 전용 타입으로 공개
- 강타입 ID: `sealed record` + `IEntityId<TSelf>`(`Value`, `static abstract From`) 인터페이스, 타입별 최소 복사(C안). 비제네릭 공통 기반은 `userId == placeId`가 컴파일되어 기각(측정: A만 컴파일, 제네릭 기반·인터페이스는 CS0019). 제네릭 기반 record는 Empty 검사 1곳이지만 상속 구조가 두 겹이라 기각. 소스 생성기는 Domain leaf 유지 위해 기각. 인터페이스 목적 = D3 제네릭 값 변환기(`T.From`). `.Equals(object)` 교차 비교는 어느 안도 못 막음
- 엔티티 동등성: 재정의 안 함(비교하는 코드 없음, 중복은 DB PK·UNIQUE). 엔티티 시각: `DateTimeOffset` 인자, `default`·비UTC는 예외(호출 경로 이탈 신호)
- 엔티티 시각 검증: `Common/TimeGuard.Utc()` 정적 가드(값 객체 `UtcTimestamp` 기각). 근거: 시각이 들어오는 길은 `Create` 한 곳뿐이라 빠뜨려도 바로 보임, 값 객체는 D3 변환기·감싸기 비용이 이득보다 큼. `[CallerArgumentExpression]`으로 원래 인자 이름 유지(측정: `ParamName`=`createdAt`)
- 리뷰 이미지는 **스토리지 키** 저장(URL 기각): CDN 도메인·버킷·환경이 바뀌어도 기존 행은 그대로, 키 → URL 변환 한 곳만 수정
- `ReviewImage`: 생성은 `internal`(Application 차단, Domain 안 다른 코드는 만들 이유 없음 → 중첩 클래스 기각). 이미지 하나 규칙만 가짐. `sortOrder`는 클라이언트가 보내지 않고 `Review`가 목록 인덱스로 매김 → 중복·빈틈이 구조적으로 불가능
- R-RV-03 기준 날짜: `Review.ReviewDate`(`DateOnly`, UTC) 속성 → `review_date` 컬럼 + `(user_id, place_id, review_date)` UNIQUE. 표현식 인덱스 기각(같은 계산이 SQL에도 생겨 규칙이 두 곳)
- SSoT 상수: 규칙을 지키는 타입이 `const`를 가진다(`ReviewContent.MaxLength`, `Review.MaxImages`) — 정적 규칙 클래스로 모으지 않음
- `Review.Images`: `List` backing field + `AsReadOnly()` 공개(캐스팅 우회 차단, 래퍼 할당은 3장 목록에서 무시) → D3-2에서 getter가 `SortOrder` 정렬한 복사본 반환으로 변경
- `Review.Create` 실패 순서: 예외(호출 실수) 먼저 → Result(회원 입력) 나중. 개별 키 검사는 장수 검사 뒤(의도된 예외, 코드 주석). 같은 스토리지 키 중복은 DB UNIQUE — D3-2에서 `UNIQUE(storage_key)`로 확정
- 그룹(D2-4 답변): 그룹명 R-GR-10(화이트리스트 + ASCII 공백, 가운데·연속 공백 허용, 이모지 제외 — 나중에 넓히는 건 싸다, 유일하지 않음). `AddMember(UserId, joinedAt)` — 초대 코드는 Application 조회에만. 검사 순서 R-GR-07(중복) → R-GR-01(정원): 회원이 바꿀 수 없는 구체적 사실 먼저. `RemoveMember`는 멱등, 반환 `enum MemberRemoval { NotMember, Removed, GroupEmptied }`(3상태 — 테스트·로그에서 멱등 구분), `GroupEmptied`면 Application이 같은 트랜잭션에서 그룹 삭제. 방장(owner) 없음(2026-10-13) — 읽는 유스케이스가 없고, 넣으면 위임·탈퇴 규칙이 따라옴. 원본 `owner_id ... on delete cascade`는 방장 회원 탈퇴 시 다른 멤버의 그룹까지 삭제하는 결함. 이력(만든 회원)은 첫 멤버의 `JoinedAt = CreatedAt`로 남음. 필요해지면 컬럼 + 백필(가장 이른 `JoinedAt`)로 추가
- Group 구현(D2-5): `Create`는 `Group` 반환, 생성자는 필드 대입만(첫 멤버 추가는 `Create`). `AddMember` → `Result`(`AlreadyMember`·`GroupFull`, 둘 다 `Conflict`). 빈 그룹 `AddMember`는 예외. `MemberRemoval`은 `Groups/MemberRemoval.cs`, `NotMember = 0`
- 도메인 루트 메서드는 TDD로 쓴다(테스트 목록 → Red → Green). 처음부터 통과한 테스트는 뮤테이션으로 실패 가능성을 확인

## 막힌 점
(없음)
