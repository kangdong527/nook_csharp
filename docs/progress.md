# 진행 내역

- 시작일: 2026-10-08
- 현재: **D2 진행 중** (2026-10-10) — 브랜치 `feat/d2-domain`(미푸시). 실패 표현 기반, 값 객체 4개(`ReviewContent`·`Coordinate`·`Nickname`·`InviteCode`), 분류표(`docs/design/domain.md`) 완료. 달력 기준 약 1일 지연
- 다음 할 일 (순서대로)
  1. ~~클래스 다이어그램~~ (2026-10-11 완료)
  2. **엔티티 8개** — 가벼운 것(`Bookmark`·`Like`·`ReviewImage`)부터. `Review`는 작성일을 `TimeProvider`로 정한다(R-RV-03)
  3. **`Group.AddMember()`** — R-GR-01(정원)·R-GR-07(중복) 미리 검사. R-GR-02는 Application, 최종 보장은 DB(D6)
  4. `/design-review src/Nook.Domain` 🔴 0 → D2 게이트 → PR

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

## 체크리스트

### 1주차 — 설계와 정합성
- [x] D1 도메인 분석·유스케이스·솔루션 골격·ADR-001
- [ ] D2 클래스·엔티티·값 객체 (design-review 🔴 0) — 진행 중: Error·Result, 값 객체 4개, 분류표 완료 / 다이어그램·엔티티 8개·AddMember 남음
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
- 리뷰 이미지 저장 형식: URL vs 스토리지 키 — D3
- 고아 이미지 판별 기준(미참조 + 생성 후 N시간)과 정리 배치 — D3
- 별 상태: 저장 컬럼 vs 조회 시 계산 — D3
- 북마크 테이블에 "북마크 여부" 컬럼이 필요한가(행 존재 = 북마크) — D3
- 중복 저장/미저장 해제 요청을 오류로 볼지 성공(멱등)으로 볼지 — D5
- 북마크 API: 토글 vs PUT/DELETE — D5
- 배포 대상(단일 VM vs Azure vs Fly.io) — D12 전까지
- 브랜치 접두사 `chore/*` 허용 시 CLAUDE.md 반영
- PR 머지 방식(merge / squash / rebase)
- SSoT 상수 위치: 지금은 `ReviewContent.MinLength/MaxLength`. 닉네임·그룹명·이미지 수가 들어올 때 값 객체별 `const` 유지 vs 도메인 규칙 정적 클래스로 모으기 — D2
- 리뷰 본문 trim 범위: `string.Trim()`은 전각 공백(U+3000)은 지우지만 폭 없는 공백(U+200B)은 남김 — 허용할지 D4 테스트에서 결정 (닉네임은 화이트리스트로 해결됨)
- `ReviewContent`에도 NFC 정규화를 적용할지 — NFD 입력은 코드 포인트가 부풀어 50자 제한에 먼저 걸림. D4에서 재현 후 결정
- 닉네임 대소문자 무시 UNIQUE 방식: `lower(nickname)` 표현식 인덱스 vs `citext` — D3. C#은 `ToLowerInvariant`, PostgreSQL `lower()`와 결과 일치 확인 필요
- R-RV-03 UNIQUE: `(created_at AT TIME ZONE ...)::date` 표현식 인덱스가 가능한가(IMMUTABLE 제약) vs `review_date` 컬럼 저장 — D3
- 정원이 찬 그룹에서 멤버가 나가면 노출된 초대 코드가 다시 유효해짐 — 코드 재발급·만료 필요 여부, GR 유스케이스 상세화 때
- ADR-002 "원본과 다른 점" 후보: 리뷰 길이 trim 후 계산, 리뷰 하루 기준 UTC(원본 "00시"), 닉네임 화이트리스트·NFC(원본은 제한 없음·UTF-16 길이), 닉네임 UNIQUE(원본은 DB 제약 없음)

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
- 리뷰 수: 같은 장소 하루 1개(R-RV-03), 기준 UTC 00:00, 작성일은 Domain이 `TimeProvider`로 결정
- 유일성 규칙(다른 인스턴스와 비교)은 저장소가 최종 보장. 애그리거트 안 비교(R-GR-07)는 루트가 미리 검사
- 규칙 ID 형식: `R-영역-두자리`
- 그룹 생애: 만든 회원이 첫 멤버(R-GR-08), 마지막 멤버 탈퇴 시 같은 트랜잭션에서 그룹 삭제(R-GR-09) — 빈 그룹 배치 정리안은 합류·정리 경합 때문에 기각. 다중성 `1..4`
- 그룹 생성도 R-GR-02 검사·R-GR-04 사용자 행 잠금을 거친다 (그룹 생성 유스케이스 상세화 때 반영)
- 탈퇴도 인원을 세기 전에 그룹 행을 잠근다(Write Skew 방지) — D6 재현 대상
- 외래 키는 유지한다. 제거는 메타데이터 변경으로 싸고, 나중에 추가하는 쪽이 기존 행 검증으로 비싸다
- 대리 키 제거: Bookmark·Like·GroupMember는 복합 키(D3에서 확정). 시간은 `DateTimeOffset`, 자식 컬렉션은 읽기 전용 타입으로 공개

## 막힌 점
(없음)
