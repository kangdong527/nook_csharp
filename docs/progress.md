# 진행 내역

- 시작일: 2026-10-08
- 현재: **DEP 진행 중 — 1·2·3·4단계 완료, 도메인 없이 할 수 있는 코드 전부 완료(2026-11-09, PR #33~#38): 7단계 코드 · 8단계 준비(arm64·운영 compose·배포/롤백 스크립트) · 9단계 로컬 리허설**. **도메인 `fillyournookwithstars.com` 구매 완료(2026-11-09, 등록 성공·호스팅 영역 NS·SOA 콘솔 확인)**. **SES 서울 리전 도메인 자격 증명 생성(Easy DKIM RSA 2048, CNAME 3개 Route 53 자동 게시, MAIL FROM·구성 세트 끔) · DMARC `p=none` 게시(공개 DNS 8.8.8.8 조회 확인) — 도메인 `확인됨`. 샌드박스 테스트(서울, 수신 개인 Gmail 이메일 자격 증명 — 처음 시드니에 잘못 만들어 삭제 후 서울 재생성): `no-reply@` → Gmail **받은편지함**, 한글 정상, SPF PASS(amazonses)·DKIM PASS(d=fillyournookwithstars.com)·DMARC PASS**. DMARC `p=quarantine`으로 올림(공개 DNS 확인) · 계정 수준 억제 목록 반송·불만 켜짐 확인 · **SES 운영 승인 요청 제출(2026-11-09, 서울, 트랜잭션, URL 도메인, English) — 결과 메일 대기(약 24시간)**. AWS가 추가 질문을 보내면 답변 초안(트랜잭션 매직 링크만·15분 3통·수신자별 상한·IP 레이트 리밋·억제 목록·하루 100통 미만)으로 답함. **5단계 시작(2026-11-09): S3 버킷 2개(계정 리전 네임스페이스, 버전 관리) · 이미지 수명 주기 35일 · IAM 역할 `nook-ec2`(SSM + 인라인 `deploy/iam/nook-ec2-inline-policy.json`, 백업 문은 사용자 작성) — CLI로 설정·본문 확인. 다음은 EC2(과금 시작)**. **카카오 JavaScript SDK 도메인에 `https://fillyournookwithstars.com` 추가(2026-11-09, 기존 `http://localhost:3300` 유지, `www` 미등록 — 필요할 때 리다이렉트와 함께)**. **다음: SES 승인 확인 후 테스트용 이메일 자격 증명 삭제 → 5단계(인프라)**. 남은 PM 확인: 처리방침 `[PM]` 5개(선택 항목 목적·위탁 고지·카카오 제3자 제공·보호책임자·시행일 — `docs/design/privacy-policy.md`, 성별·생일을 필수로 원하면 쓰는 기능부터) 확인 → 도메인 구매 → 5단계(인프라)**. 개인정보 정책 결정·구현은 PR #40(2026-11-09). 남은 작업 추정(Claude) 약 5~6 작업일. DEP 계획·기록은 **`docs/design/dep-plan.md`**(4단계 실제 진행은 §6), AWS 구성은 **`docs/design/aws-account-setup.md`**, 종료 절차는 **`docs/design/aws-shutdown-runbook.md`**(서비스 완전 종료 전체 프로세스, 2026-11-08 재구성). 달력 기준 약 10일 지연. **2026-10-27부터 서비스 계층 `TODO(human)`도 Claude 작성(사용자 요청 — 면접·역량검사 대비 기간, 사용자가 말할 때까지)**
- **일정 변경 채택(2026-10-20)**: D5 → D6 → FE 프론트 연결 → DEP 배포 주간(1주, AWS EC2 + GitHub Actions) → README 최소판 → D7~D11 → D13 → D14. 상세는 `docs/개발_프로세스.md` §5 "진행 순서"
- **▶ 다음 세션 재개 지점 (여기서 바로 시작)**
  0. Docker Desktop을 먼저 켠다(꺼져 있으면 통합 테스트 전체가 `DockerUnavailableException`). 손 테스트 뒤 `dotnet run`을 끄지 않으면 빌드가 DLL 잠금(MSB3027)으로 실패한다
  1. **▶ DEP (여기서 바로 시작)** — 계획 `docs/design/dep-plan.md`(단계 1~10, 결정 1~6, Pillar 점검표, 함정, §5 개인정보). 보안 1순위·비용 2순위
     - **▶ 2026-11-09 밤 이후 재개 (여기부터)** — ① `feat/dep5-infra` PR(`dbd723e` 이후 커밋 포함) ② ~~SES 운영 승인~~ ✅ 2026-11-09 당일 승인(배너 사라짐, 일일 할당량 200 → 50,000). 남은 것: 서울 테스트용 개인 이메일 자격 증명 삭제(선택, 무료) ③ **6단계 비밀값**(Parameter Store 이름 체계·인스턴스 역할 `ssm:GetParameters` 추가) ④ **5단계 C** EC2 t4g.small(AL2023 arm64, 키 페어 없음, IMDSv2 홉 2, EBS 20GB gp3 암호화, SG 80·443 내 IP만, 역할 `nook-ec2`) → D 탄력적 IP + A 레코드 → E SSM 접속·Docker → dep-plan §4 "5단계 실호출 확인 목록" ①~⑤. AWS 작업 안내는 **단계마다 계정·리전 확인을 첫 줄에**
     - **세션 시작하면 할 일 (순서대로)**
       0. **2026-11-09 한 일**(아래 세션 기록): Dependabot 잔여 #17·#18 닫음 · 등록처 Route 53 · S3 이미지 저장소(`IImageStorage`, PR #33) · ForwardedHeaders + IP RateLimiter(PR #34) · `/health/live`·`ready`, 보안 헤더·강제 CSP(PR #35) · BFF `/api/auth/*` Origin 검사·429 안내·백업/복원 리허설·arm64 빌드·`compose.prod.yaml`·`deploy.sh`(PR #37) · A1 수신자별 메일 상한·SES 발송기 코드(PR #38). 단계별 남은 것은 dep-plan 단계 표 7·8·9행과 §4
       - 로컬 compose가 배포 리허설 이미지(`nook-local-*:good`)로 떠 있을 수 있음 → `docker compose -f deploy/compose.yaml up -d --build`로 최신 코드로 다시 올린다
       1. `git status` — `main`에서 시작(마지막 병합 PR #35, `519f3fa`). 작업 트리 깨끗. 새 클론이면 `git config core.hooksPath .githooks`. GitHub 배너 "main branch isn't protected"는 무시(3단계 결정 — private·Free라 규칙 강제 불가, 공개 전환 시 켬)
       2. ~~4단계 AWS 계정 보안~~ ✅ — 서울 리전(원인: 통합 설정 "표시되는 리전", SCP와 별도) · S3 계정 퍼블릭 차단 · Identity Center 권한 세트 없음(Builder ID 소스) → 대안 nook-prod IAM 역할 `nook-readonly` + `aws login`(CLI 2.39.5) · CLI 읽기 검증 9항목(`aws-account-setup.md` 9절) · nook-prod 루트 자격 증명 "없음" · 계정 이름은 이미 `nook-prod`(세션 표시만 옛 이름). ~~남은 것: `nook-readonly` 권한 축소, 시드니 정리~~ ✅ 2026-11-09(`ViewOnlyAccess + SecurityAudit`, SCP·표시 리전에서 시드니 제거 — 시드니 호출이 SCP 명시적 거부로 실패하는 것 실측)
          - CLI 사용: 조회는 `--profile nook-readonly`, 만료 시 `aws login --profile nook-prod`. Claude는 읽기 명령만
       3. ~~Dependabot 정리~~ ✅ 2026-11-08 — #16(NuGet 묶음)·#13(npm 묶음) 병합, typescript 7·eslint 10은 ignore 추가(#21, 해제 조건 주석) → Dependabot이 자동으로 닫음. Test.Sdk 18·coverlet 10은 #16과 충돌 후 `rebase`/`recreate` 명령에 Dependabot이 프로젝트별 8개(#22~#29)로 쪼개 다시 열어서 → **Claude가 한 PR(#30)로 직접 올려 병합**(로컬 빌드·단위 132, CI 통합 포함). 남은 Dependabot 브랜치 2개(#17·#18, 내용은 이미 `main`)는 2026-11-09 사용자가 닫고 브랜치 삭제 ✅
       4. ~~도메인 구매~~ ✅ 2026-11-09 — **`fillyournookwithstars.com`**(Route 53, $16/년, 1년·자동 갱신·개인정보 보호, 등록자 이메일 인증 완료, 요청 `성공`, 호스팅 영역 NS·SOA 콘솔 확인). `.com`은 HSTS 프리로드 아님 → 5단계에서 HTTP→HTTPS·HSTS 직접 확인(dep-plan 결정 5). 런북 과금 표 2줄 추가(도메인·호스팅 영역). 아래는 구매 전 메모: 콘솔 구매는 사용자, Claude는 한 단계씩 안내(Register domains에서 가격 확인 후 멈춤). CLI 조회는 `--region us-east-1`(route53domains 전용), `aws`는 `C:\Program Files\Amazon\AWSCLIV2\aws.exe`(PowerShell PATH에 없음). 구매 직후: 개인정보 보호 켜기, 자동 갱신 켜기, **등록자 이메일 인증 15일 내**, 런북 과금 표에 추가. 일찍 시작할 것: SES 도메인 인증·운영 승인 요청(약 24시간 대기), 카카오 도메인 등록
       5. 일정 추정(2026-11-08, Claude): 남은 DEP 약 8~10.5 작업일 → 2026-11-09에 S3 구현(결정 3a, 코드)과 7단계 코드 일부 완료. 남은 7단계: SES 발송기·메일 상한(도메인 필요), 개인정보 처리방침·수집 동의(§5), BFF `/api/auth/*` Origin 검사, 429 화면 문구, CSP nonce
       6. 5단계 때 챙길 것(dep-plan §4, 2026-11-09 추가): compose `ImageStorage__Provider: S3` + **api만** egress 네트워크 · EC2 IMDSv2 **홉 제한 2** · 인스턴스 역할 `s3:ListBucket`(없으면 없는 키 403 → 500) · compose 네트워크 설정을 바꾸면 `down` 후 `up`(DNS 별칭 빠짐) · 배포 헬스체크는 `docker run --rm --network nook-deploy_backend curlimages/curl http://api:8080/health/ready`
     - AWS 구성 전체 정리: **`docs/design/aws-account-setup.md`**(계정·프로젝트·팀·세션·신원 구분, 작업별 어디로 가나, 오늘 한 일, SCP 현황)
     - AWS 작업 원칙(이번 세션 교훈): 콘솔이 새 시작 경험이라 Claude가 아는 화면과 다름 → **한 단계씩, 화면 문구를 먼저 받고** 안내. 비밀값·MFA 코드·시크릿 키는 대화에 붙이지 않음. 리소스를 만들면 `aws-shutdown-runbook.md` "실제로 만든 과금 대상" 표에 추가. 리소스 작업 전 **계정·리전** 확인
     - 완료(2026-11-07): 1단계 Next 16(`37de343`, PR #10) · 2단계 컨테이너화(PR #11, ADR-003 Claude 작성 — Provider 설정값 선택·migrate 1회성 컨테이너·edge/backend 네트워크·비루트·Origin `NOOK_PUBLIC_ORIGIN`) · 3단계 CI(`.github/workflows/ci.yml` api·web·audit·images, Dependabot 루트, `.githooks/pre-commit`, lint 오류 5건 수정, Trivy 대응으로 api·migrate chiseled-extra·web npm 제거)
     - 결정(2026-11-07, 사용자): **결정 6** 5단계 SG는 내 IP만 → 7단계(보안 + 개인정보 처리방침·수집 동의) 완료 뒤 전체 공개 · lint 오류 = CI 실패 · `main` 보호는 규칙 없이 CI 표시 + 로컬 훅(private·Free 제약) · Claude 커밋 전 스테이징 경고 훅(`.claude/hooks/staged-check.js`)
     - 로컬 compose 사용: `cp deploy/.env.example deploy/.env`(값 채우기) → `docker compose -f deploy/compose.yaml up -d --build` → http://localhost:3300. **`npm run dev`를 끄고**(같은 3300을 IPv6로 가로챔) `netstat -ano | findstr :3300`에 127.0.0.1만 남는지 확인. 로그인 링크는 `docker compose -f deploy/compose.yaml logs api`. api 컨테이너는 셸 없음(chiseled) → 파일은 `docker compose -f deploy/compose.yaml cp api:/app/data/images ./tmp-images`
     - 백로그(원본 범위 밖, PM 결정): X1 리뷰 사진 순서 바꾸기 · X2 그룹원 보기 → `docs/design/fe-backend-backlog.md` §5
     - 여전히 미결: `web/` 테스트 러너(incident 004 §7), 토큰 수명 60분 유지 vs 7일/refresh, 개인정보 정책 결정(만 14세 미만·선택 항목 동의·파기 범위, dep-plan §5). ~~이미지 저장소 인터페이스 이름~~ → `IImageStorage`(2026-11-09)
     - 7단계 메모: `/api/auth/{signup,logout,magic-link}`에 Origin 검사 없음 · DataProtection 경고(JWT만 써서 무영향) · ~~`UseHttpsRedirection` 경고~~ 제거, 로그 0건 실측 · ~~ForwardedHeaders~~ ✅
  2. **FE** (완료 — PR #9, 2026-10-28) (순서 사용자 확정, 2026-10-26) — 범위 확정(2026-10-27 PM): **원본 화면 전체**. 백엔드 추가 API가 늘어 일정 재검토 필요(사용자). 진행 순서는 그대로 로그인·지도/별 상태·리뷰 작성부터
     - 결정(사용자): 같은 리포 `web/`(원본 리포는 Supabase판으로 보존), TypeScript + Next.js 14 그대로, React Native 변환 안 함. 개발 포트 **3300**(카카오 도메인 등록 기준 → CORS도 3300)
     - 연결 구조: 브라우저 → C# API → Docker PostgreSQL. 프론트는 DB를 모른다. 끝나면 `@supabase/*`·`NEXT_PUBLIC_SUPABASE_*` 전부 제거
     - ~~`web/.env.local`은 원본 Supabase 것~~ → FE-2에서 Supabase 제거. `NEXT_PUBLIC_SUPABASE_*` 줄은 쓰이지 않으니 사용자가 지운다(남아도 동작 영향 없음)
     - ~~가져오기 커밋~~ ✅ `73a14db` · 루트 `.env*.local` 무시 `508de04`
     - ~~`npm audit fix`~~ ✅ `48bb561`(lockfile만, `tsc`·`next build` 통과. 남은 것은 next critical 1건 → DEP 전 업그레이드)
     - 백엔드 할 일 전체 목록: **`docs/design/fe-backend-backlog.md`**(원본 함수 28개 대조, 제안 순서 1~7, 결정 기록). 순서·일정은 사용자 결정 — 아래는 그 3번부터
     - ~~백엔드 선행 과제 1·2·3·4~~ ✅ FE-2 — `GET /me`(AU-04) · `GET /reviews/{id}`(RV-04, 작성자만·403) · 이미지 A안 결정 · 토큰 BFF 결정 · `IsBookmarked`(PL-02) · 가입 성별·생일(R-US-04·05)
     - ~~FE 인증 교체(BFF)~~ ✅ FE-2 `47e8ffd` — route handler `/api/auth/{magic-link,consume,signup,logout}`, 쿠키 `nook_at`(1시간)·`nook_signup`(15분), 링크 도착 `/auth/magic-link`(클라이언트가 `#token` 읽음), `GET /me` 기반 3상태 가드, middleware는 쿠키 유무만. 로컬 API·DB로 가입·로그인·링크 재사용 400·로그아웃·가짜 쿠키 13단계 확인
     - ~~BFF 중계 + 지도·별 상태~~ ✅ FE-2 — `/api/nook/[...path]`(허용 목록·헤더 정리·Origin 검사·경로 조작 거절), `lib/api/client.ts` `nookFetch`(401 → /login). `store.ts` `getMySavedPlaces`·`upsertPlace`·`getPlace`·`isBookmarked` → C# API, `toggleBookmark` → `setBookmarked(placeId, on)`(PUT/DELETE 멱등). 13항목 확인(허용 밖 404, Origin 없음·외부 403, `..%2F` 404, 쿠키 없음 401)
     - ~~리뷰 흐름 연결~~ ✅ FE-2 — 이미지 표시 A안 `7f93f8f`(로컬 디스크 `App_Data/images`, 익명 `GET /images/{key}`, 응답 `Url` = `/api/nook/images/…`) · 리뷰 6함수 `863cea1`(목록 커서 무한 스크롤, multipart 작성·수정 + version, 좋아요 목표 상태, RV-04 403) · **R-RV-03 하루 기준 UTC → KST 00시** `e663bfb`(사용자 결정, `ReviewDay`). 로컬 API로 11단계 확인. **데모 3흐름(로그인·지도·리뷰) 연결 완료 — 원본 함수 28개 중 15개**
       - 수동 확인 메모: `curl` 체인에서 `grep -c`가 0이면 종료 코드 1 → `&&` 뒤가 안 돈다. 같은 이메일 링크는 15분에 3개(`MagicLinkPolicy`). Next는 `npm --prefix web run dev`로(루트에서 `npx next`는 next@16을 받아 실행한다)
       - 알려진 빈 곳: 저장한 장소를 다시 열면 카테고리·전화가 비어 보임(`places` 부가 정보 없음, D3-2), 리뷰 카드 작성자 사진은 기본 이미지(PF-01 때). 로컬 개발 DB의 기존 `review_date`는 UTC 기준 값(운영 데이터 아님)
       - 브라우저로 직접 눌러 본 적 없음(원본 Supabase 보호) — 카카오 키·`.env.local` 교체 뒤 화면 확인 필요
     - ~~프로필 묶음~~ ✅ FE-2 — PF-04 `GET /me/reviews`(키셋 5개)·PF-03 FE가 `/me/places`로 셈 `b1a6e79` · PF-01 `PATCH /me`(닉네임, 미리 확인 없음 — DB UNIQUE)·`PUT/DELETE /me/avatar`(`users.avatar_key`, `avatars/` 키) `a5a6f0e`. 원본 모달엔 사진 삭제 버튼이 없어 DELETE는 미사용(정정)
     - 그룹 묶음 (PM 허가: 만든 사람이 방장 — ADR-012, ADR-004 결정 6 번복)
       - ~~1. 방장 + 색 슬롯~~ ✅ `ed28fbc` — `groups.owner_id`(RESTRICT + 인덱스), 방장이 나가면 가장 먼저 합류한 멤버로, `group_members.color_slot` 0~3 랜덤·UNIQUE. 원자성 테스트는 임시 CHECK로 바꿈(owner FK가 groups INSERT를 먼저 막아서)
       - ~~2. GR-03 이름 변경(방장만, 그룹 행 잠금 안)·GR-06 `GET /me/groups`(SQL 1문장 — 로그로 확인)~~ ✅ `ea94b3f`
       - ~~3. GR-05 그룹 지도~~ ✅ `237d1f8` — 장소마다 1행 사실(리뷰 멤버 수·첫 리뷰어 슬롯·북마크 수·단독 북마커 슬롯), SQL 1문장, 멤버만 403 · FE 그룹 6함수 `403bdd6`(슬롯 → 원본 팔레트 임시 매핑 `slotColor`, PM 별 그리기 `groupStarSvg`, 그룹명 2~6자, 이름 변경 UI 방장만). 로컬 API로 두 회원 흐름 확인(합류·403·방장 넘김)
     - ~~탈퇴 PF-02~~ ✅ — 스키마 `9d4440e`(reviews·likes `SET NULL`, likes 대리 키 `id` — 복합 PK는 NULL 불가) · `DELETE /me` `33f84cd`(회원 행 FOR UPDATE → 그룹마다 나가기 경로 → 링크 기록 삭제 → DELETE, 커밋 뒤 사진 삭제, 남은 토큰 쓰기 = 자식 쪽 FK 위반만 401) · FE `2683756`(BFF `/api/auth/withdraw`, "탈퇴한 사용자"). PM 답: 복구 없음, 좋아요 유지
     - ~~Supabase 제거~~ ✅ `795dbb3` — 패키지·어댑터·탐침·환경변수. **원본 함수 28/28 연결, 화면 코드에 Supabase 없음**
     - ~~그룹 지도 버그~~ ✅ 2026-10-28 `0bf1e24` · `docs/incidents/004`(Claude 작성, 사용자 요청) — H1(응답에 있음)·H2(슬롯 0도 정상 그림) 기각, **H3 확정**: 다른 멤버의 변경을 다시 불러오는 경로가 없었음(원본도 같음). 해결 A안(사용자 결정): `visibilitychange`+`focus` 때 다시 불러오기, 1초 안 중복 1회(탭 복귀 1번 = 요청 1건 실측). 미확인: 중복 방지 변이, `reloadSaved` 늦은 응답 덮어쓰기 경합, `web/` 테스트 러너 없음(DEP 때 결정)
       - ~~jamME 탈퇴 뒤 옛 "쥑쥑"이 없어진 것~~ 정상 — 탈퇴 때 방장이 hwi로 넘어온 것 사용자 확인, 그 뒤 그룹이 정리됨
     - ~~브라우저 확인~~ ✅ 2026-10-28 — 이전 A1~9·B10~16 전부 정상(사용자), 회색 별만 버그였음. 회귀 확인용 목록 `docs/design/fe-manual-checklist.md`(재구성)
     - ~~`feat/fe-connect` 푸시·PR~~ ✅ PR #9 병합, 브랜치 정리(사용자). 병합 전 검증: `dotnet build` 경고·오류 0, `dotnet test` 348(1·124·7·216), `next build` 성공
     - ~~다음: DEP 1일째~~ → 2026-11-05 ①(Next 업그레이드 먼저) 선택, 위 "DEP"로 이동. 당시 기록: 첫 결정(사용자): ① Next 보안 업그레이드 먼저(npm audit critical next 1·high glob 3 — 공개 서버에 취약 버전을 안 올림) vs ② CI 골격 먼저(업그레이드 PR부터 자동 빌드·테스트 보호). 업그레이드 뒤 비교할 기준선: First Load JS 공통 87.3 kB · `/review/new` 96.3 kB · Middleware 26.6 kB. 함께 정할 것: `web/` 테스트 러너(incident 004 §7), 토큰 수명 7일/refresh, `web/.github`·`.githooks` 정리
       - 실행: Docker → `dotnet run --project src/Nook.Api --launch-profile http` → `npm --prefix web run dev`(루트에서 `npx next` 금지 — next@16을 받아 실행) → 메일 링크는 API 콘솔. 사진은 `src/Nook.Api/App_Data/images/`(git 제외). 로컬 DB에 Supabase 데이터 없음(이관한 적 없음 — 회원 10명: 확인용 `@nook.test` 9 + 사용자 `jam`)
       - 알려진 빈 곳: 저장 장소 다시 열면 카테고리·전화 비어 보임(D3-2 `places` 부가 정보 없음), 그룹 색은 임시 팔레트(디자인 대기), 고아 이미지 정리 배치 미구현(`reviews/`·`avatars/`)
       - ~~그다음: `feat/fe-connect` 푸시·PR(사용자) → DEP(Next 업그레이드)~~ ✅
     - 결정(FE-2, 사용자): BFF라 브라우저가 C# API를 직접 부르지 않음 → CORS 설정 불필요할 가능성(이미지 `<img>`는 CORS 대상 아님). 토큰 수명 60분 유지(refresh 없음, DEP 전에 7일 연장 또는 refresh 재검토)
     - 정리 대상: `web/.github`·`web/.githooks`는 하위 폴더라 동작 안 함(원본 GitHub과도 무관) → DEP에서 CI를 루트로 합칠 때 정리
  - **D6 남은 것** (완료, PR #8)
     - ~~incident 002 미결 ②~~ ✅ D6-2 — 바디 필드 `Version`(응답 `ReviewView`·`PlaceReviewView`, RV-02 폼 필수). ADR-011·usecases.md RV-02(3d·4d·6c)·PL-02·erd.md reviews version·ADR-009 결정 2 반영(Claude 작성, 사용자 요청)
     - ~~incident 002 미결 ①: 변이 6/15~~ ✅ D6-2 — `OrderedGateStorage`(랑데부 + `TaskCompletionSource` 게이트)로 커밋 순서 고정, Theory 2케이스. (D6-1 탈퇴·합류 분포 테스트에도 같은 장치 적용 가능 — 필요하면 사용자 결정)
     - ~~GR-01·02·04 API 엔드포인트~~ ✅ D6-2 — `POST /groups`(201 + Location + `GroupView`), `POST /groups/join` 본문 `{ inviteCode }`(200 + `GroupView`), `DELETE /groups/{id}/membership`(204, 멱등). 이월: 그룹 조회 API(내 그룹 목록·상세)는 유스케이스에 없음 — FE 연결 때 필요하면 사용자가 유스케이스부터
     - ~~ADR-010 "후속" 반영~~ ✅ D6-2 — "후속 결정" 절(잠금 순서·강도·탈퇴·생성)
     - ~~이월 메모: 그룹 조회 API~~ → D6-3에서 원본 조사, 판단은 PM 확인 후("PM 확인 항목" 2)
  2. ~~learning-log D6-1·D6-2~~ ✅ Claude 작성(사용자 요청). 예전 것: learning-log D5-7·D5-8 확인, ADR 권장 3건(메일 폭탄 방어 겹·단일 소비자 전제 / ADR-001 결정 3 보완 SharedKernel / **"장소는 삭제하지 않는다" 불변식**)
  3. ~~D6 푸시·PR·병합~~ ✅ PR #8. PM과 "PM 확인 항목" 1~4 의논은 남음
- 다음 할 일 (D5, 순서대로)
  1. ~~인증 골격 커밋 + 문서~~ ✅ `a9ce0d9` · `c1f8539`
  2. ~~`magic_link_tokens` 마이그레이션~~ ✅ `AddMagicLinkTokens`(Claude 작성, 사용자 요청). 영속 클래스 `Persistence/Auth/MagicLinkToken`(internal, DbSet 없음) + Configuration, 식 인덱스는 `migrationBuilder.Sql`. CHECK 3개 통합 테스트 추가(Integration 21개). 로컬 DB 적용·`\d`로 erd.md와 일치 확인
     - 이월: Q2가 `ix_magic_link_tokens_email_lower_created`를 타는지 `EXPLAIN`(빈 테이블이라 지금은 의미 없음) — D8
  3. ~~AU-01·AU-02 유스케이스 + `POST /auth/magic-link`, `/auth/magic-link/consume`~~ ✅ `c11794a` · `720f388`. 메일은 콘솔 발송기 + 백그라운드 워커(A1·A3는 DEP 5일째)
  4. ~~개발 전용 토큰 발급 엔드포인트~~ 기각(사용자 결정) — 운영 노출 사고 패턴. 자동 테스트는 DI의 `AccessTokenIssuer`로 토큰 생성, 손 테스트는 매직 링크 + 콘솔 발송기
  5. ~~`WebApplicationFactory` API 테스트(D5 게이트: 정상·실패 흐름) — 401·Fallback 정책 포함~~ ✅ `dc67487`
  6. 나머지 D5 API: ~~PL-01~~ ✅ `485133c` · ~~R-STAR~~ ✅ `df3da5b` · ~~BM-01·02~~ ✅ `bc95bd2`(경로 `Guid.Empty` → 500 유지, 사용자 결정) · ~~LK-01~~ ✅ `52481f0` · ~~RV-01~~ ✅ `e239562` · ~~RV-02~~ ✅ `2e9b825`·`3aa20c7` · ~~RV-03~~ ✅ `cc242c7` (수정·삭제도 D5에 포함, 2026-10-23 사용자 결정) · ~~BM-03~~ ✅ `3a4d2df` · ~~PL-02~~ ✅ `a3fe19c` (조회 — "프론트가 부를 API 전부", 2026-10-23 사용자 결정. PL-02는 N+1 그대로 두고 D9에서 측정·개선)
  - 확인 못 한 것: Production 환경에서 `IncludeErrorDetails = false`로 `error_description`이 빠지는지(환경변수로 키를 넣어야 함) — DEP 5일째
- D4 할 일 (완료)
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
  5. ~~D4 게이트 누락분~~ ✅ `ReviewContent.Create` 경계 9·10·50·51자(+trim·안쪽 공백·이모지), `Review` 이미지 3장, DB CHECK 11개 통합 테스트 `CheckConstraintTests`(raw SQL UPDATE → 23514 + `ConstraintName`, 16케이스). 변이: 상한 상수·char 수·trim 제거·마이그레이션 CHECK 완화 모두 빨간불
  6. ~~D4 미결 결정~~ ✅ 프로브로 재현 후 결정(추천안 채택, 아래 "확정된 결정 (D4)") — 실패 테스트 먼저(6개 빨간불) → 구현 → 전체 103개 초록
  - 선택: `Review` 수정(RV-02, 기존 `ReviewImageId`와 새 키가 섞인 목록) — D5로
  - ~~미답: Configuration 주석~~ ✅ A안 — PK·UNIQUE·인덱스마다 `// 받는 쿼리: erd.md <table> Q..` (Q 번호만, 근거는 erd.md). 마이그레이션 SQL 제약 2곳도 동일. 인덱스·쿼리를 바꾸면 erd.md 표와 함께 고친다
  - ~~미답: namespace 분석기~~ ✅ `IDE0161`은 namespace 모양(파일 범위/블록) 규칙이라 사고와 무관 → **IDE0130**(폴더 불일치)+**CA1050**(전역 namespace) `error` + `EnforceCodeStyleInBuild`. 참조 없는 탐침 파일로 각각 단독 빌드 실패 확인. 파일명≠타입명(D2-4 `Reviews/UserId.cs`)은 여전히 못 잡음
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

### 2026-10-18 (D5-1)
1. `feat/d5-auth` 브랜치, 인증 골격부터 시작
2. 인증 방식 결정(아래 "확정된 결정 (D5)") — 소셜 로그인 확장성 검토: 발급 주체·토큰 구성은 걸림돌이 아니고, 회원 식별자(`sub`)와 로그인 수단 분리가 관건
3. JWT 검증 골격: `JwtOptions`(ValidateOnStart), `AccessTokenIssuer`(발급 한 곳), JwtBearer(알고리즘 고정·iss·aud·ClockSkew 30초·`MapInboundClaims = false`). Api는 Domain을 못 봐서(ADR-001 결정 3) 발급기는 `Guid`를 받는다 — 처음 `UserId`로 써서 빌드 실패로 확인
4. 보안 조사(OWASP JWT·Authentication·Forgot Password, RFC 8725) → Fallback 정책(기본 잠금)·`IncludeErrorDetails` 개발 한정 추가. 실측: 토큰 없음·엉터리 토큰 200 → 401, 정상 토큰 200, 만료 토큰 401 + 개발에서 사유 표시, `/openapi` 200
5. 서명 키: CSPRNG 48바이트 Base64 → user-secrets(`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`, 평문, Development에서만 로드). `openssl`이 PowerShell에 없어 실패 → `RandomNumberGenerator`로 대체
6. 매직 링크 저장 위치: 가입자는 링크 요청 시 닉네임이 없어 users 행을 만들 수 없다 → 별도 테이블(사용자 답). 사용자 초안 `| 이메일 | 인증 여부 | 토큰값 |` → 해시·만료·소모 시각·재요청·클릭과 닉네임 제출 사이 상태 5가지 검토 후 erd.md `magic_link_tokens`
7. 질문: 상태 없는 JWT만으로 링크를 처리할 수 없나 → 가능하지만 1회 사용·무효화·속도 제한을 잃고, 메모리 캐시는 재시작·다중 서버에서 깨진다(상태를 옮길 뿐)
8. `AddMagicLinkTokens` 마이그레이션: 모델(영속 클래스 + Configuration) → `migrations add` → 식 인덱스 SQL 보완 → `database update` → `\d`로 erd.md와 일치 확인. 실행 중인 Api가 DLL을 잠가 빌드 실패 → 서버 종료 후 진행
9. 질문: `.cs`(변경분 `Up`/`Down`, 손으로 고침) vs `.Designer.cs`(마이그레이션 ID 속성 + 그 시점 모델 사진, 자동 생성) vs `ModelSnapshot.cs`(최신 사진, `migrations add`의 비교 대상 — DB가 아님). 손으로 넣은 SQL은 어느 사진에도 없다

커밋: `b7909e1`(progress) · `a9ce0d9`(JWT 골격) · `c1f8539`(인증 결정·erd 설계) · 이 세션 마무리 커밋 2개(마이그레이션, progress·learning-log)
Claude가 작성: `JwtOptions`·`AccessTokenIssuer`(`BuildClaims` 제외)·`Program.cs` 인증 설정, 서명 키 생성·설정, 보안 조사, erd.md `magic_link_tokens` 설계 전체, usecases.md AU-01~03 수정, `MagicLinkToken`·Configuration·`AddMagicLinkTokens`·CHECK 테스트 3개 — 모두 사용자 요청. 사용자 작성: `BuildClaims`(`sub`만, `jti` 보류)
미답 질문(다음 세션): ~~Q1을 SELECT 후 UPDATE로 나누면?~~ ✅ D5-2: 동시 2요청 모두 미커밋 NULL을 읽어 토큰 2개(TOCTOU). 한 문장 Q1은 행 잠금 대기 후 최신 버전으로 WHERE 재검사 → 0행 → 앱이 0행을 "무효 링크"로 처리해야 함. 꼭 나누면 `SELECT ... FOR UPDATE`(같은 트랜잭션, 잠금 동안 외부 I/O 금지) / ~~`TryParse`가 실제로 막는 경우는?~~ ✅ D5-2: 위조 sub는 서명 검증에서 401(엔드포인트 미도달 — 인증 미들웨어는 익명 표시, 인가 Fallback이 Challenge). 서명 통과 + 비Guid sub는 발급기 버그·키 공유 설정 실수뿐이고, 키 유출이면 공격자는 올바른 Guid를 넣어 TryParse가 못 막음. 읽는 쪽 실수(`ClaimTypes.NameIdentifier` + `MapInboundClaims = false` → null)는 TryParse→401이면 정상 회원 전원 401로 버그를 숨김

### 2026-10-20 (D5-2)
1. 미답 질문 1(Q1 분리): 동시 2요청 모두 미커밋 NULL을 읽어 토큰 2개(TOCTOU). 한 문장 조건부 UPDATE는 행 잠금 대기 후 최신 버전으로 WHERE 재검사(EvalPlanQual) → 0행. 앱은 0행을 "무효 링크"로 처리해야 함. 꼭 나누면 `SELECT ... FOR UPDATE`. 사용자가 질문 1~5 중 3·4·5를 스스로 답함
2. 미답 질문 2(`TryParse`): 위조 sub는 서명 검증에서 끝남(인증 미들웨어는 익명 표시만, 인가 Fallback이 Challenge → 401). 서명 통과 + 비Guid sub = 서버 버그 → 예외·500(사용자 결정, "확정된 결정 (D5)"). `BuildClaims` 주석 사용자 수정. 2a(서명 주체 = `AccessTokenIssuer`, JwtBearer는 검증만)·2b(`ClaimTypes.NameIdentifier` → null) 힌트 3단계까지
3. AU-01 계층 설계: 계약은 쓰는 쪽(Application)에 — 처음 답 `List<IMagicLinkToken>`(데이터)·`IEmailSender`→Api는 오답, 힌트 3단계(시그니처) 후 "핸들러가 구현 클래스를 받으면 DIP가 깨진다"를 사용자가 설명. CS F1 약점과 같은 패턴 → 퀴즈로 재확인 필요
4. 결정: 백그라운드 발송 B안(`Channel` + `BackgroundService`), 이메일 형식 오류는 400(DB를 안 봐 가입 여부 비노출 — 사용자 답)
5. 빌드 실패 `CS0012`(Api가 Domain `Result`를 못 봄) = ADR-001 결정 3이 예상한 누출 → A안 `Nook.SharedKernel`(Result·Error·ErrorType) 분리. Domain은 더 이상 leaf가 아님(→ SharedKernel)
6. 질문: SharedKernel = AOP? → 아님(AOP는 동작을 끼워 넣기·실행 시점, Shared Kernel은 타입 위치·참조 구조). Infrastructure가 별도 프로세스? → 아님(프로젝트 = DLL, Api 프로세스에 로드, 워커는 `IHostedService` 싱글턴, 같은 메모리라 Channel이 작동·재시작 시 유실). 큐에 넣는 건 Application(`IMagicLinkMailQueue` 계약만 앎), 방법은 Infrastructure
7. 검증: `dotnet build` 오류 0(경고 3 = `TODO(human)` 미사용 매개변수), `dotnet test` Domain 84·Algorithms 1 통과, Integration 21 Docker 꺼짐으로 미실행

커밋: 없음(작업 트리에 남김 — 다음 세션 TODO 구현 후 커밋)
Claude가 작성: `Nook.SharedKernel` 프로젝트·`Result`/`Error` 이동·using 일괄 수정·참조 추가, Application `IMagicLinkTokenStore`·`IMagicLinkMailQueue`·`MagicLinkPolicy`·`MagicLinkSecret`·`RequestMagicLink`(뼈대), Infrastructure `MagicLinkTokenStore`(Q2 SQL·INSERT)·`MagicLinkOptions`·`ChannelMagicLinkMailQueue`·`MagicLinkMailWorker`·`IEmailTransport`·`ConsoleEmailTransport`·DI 등록·`FrameworkReference`, Api `AuthController`·`Program.cs` 등록·`appsettings.Development.json` `MagicLink`. 결정은 사용자: B안, 형식 오류 400, SharedKernel A안, sub 형식 위반 = 500
ADR 권장(작성은 사용자): ADR-001 결정 3 보완 — "Result·Error는 SharedKernel로, 엔티티 차단은 유지"
미답 질문(다음 세션): `mail.Enqueue`를 INSERT 앞에 두면 무슨 일이? / Q2-INSERT 사이 동시 요청(TOCTOU) 허용할지

### 2026-10-20 (D5-3)
1. AU-01 `HandleAsync` 구현(사용자). 리뷰에서 나온 것: `_ = TryGetValue` 결과 버림 → 형식 오류가 통과, `AddAsync` 미`await`(fire-and-forget이면 INSERT→Enqueue 순서가 보장 안 됨), 끝의 `throw` 잔존, 형식 오류를 `throw`로 처리(→ Result 실패여야 400), `Result.Failure(error)` 앞 `return` 누락(경고 없이 컴파일됨)
2. CS8604가 남은 이유: `[NotNullWhen]`은 메서드 **반환값**에 묶임 — `error != null` 검사로는 `email`을 추론 못 함 → `if (!TryGetValue(...)) return error;`
3. TOCTOU 타임라인: 2개 발급 상태에서 A·B 동시 COUNT=2 → 둘 다 통과 → 4개. READ COMMITTED 트랜잭션만으로는 못 막음(미커밋 INSERT가 안 보임). 한도는 여러 행 조건이라 CHECK로 이중화 불가 — `MagicLinkPolicy`가 Application에 있는 이유
4. 사용자 지적: A안의 진짜 위험은 DB가 아니라 메일 발송(동시 100요청 → 100통). 보완 후보 A1~A4, 이메일을 바꿔 가는 공격은 IP 기준 제한이 별도로 필요
5. 사용자 조사: 실무 = IP 기준 입구 제한 + 계정 한도는 DB/Redis. "단일 인스턴스라 노드별 집계 무관"은 A1·A2 모두에 해당해 둘을 가르는 근거가 아님 → 가르는 기준은 막는 위치(피해 지점 vs 입구)·생산자 추가 시·본문 필요 여부. 발송 지점 집계가 드문 이유 = 실무 발송은 다중 소비자라 무경합 전제가 없음. A3만으로 단일 IP 폭주는 막히고 남는 건 분산 공격 → ② A3 + Q2 + A1 결정
6. 일정 변경 채택 → `개발_프로세스.md`·`progress.md`·`schedule-option-portfolio-first.md` 반영
7. 핸들러 단위 테스트 7개. 사용자 예측: 전체 결과 ✗(시계 어긋남으로 `UsesSingleNow` 실패 예상 — 핸들러는 주입된 시계만 쓰므로 고정 시계에선 안 어긋남), `>=`→`>` 변이 ✓(3개 상태 테스트만 빨간불). 구멍 발견: 고정 시계는 "시각 두 번 읽기"를 못 잡음 → 부를 때마다 1초 가는 `TickingTimeProvider`, 변이(INSERT 전 `GetUtcNow()` 재호출)로 빨간불 확인
8. 손 테스트(Postman, 로컬 compose DB): 같은 이메일 5회 → 링크 3번·행 3개, 예측 일치
9. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 113개 통과. 커밋 1은 별도 worktree에서 단독 빌드·테스트 106개 통과 확인

커밋: `b55f030` SharedKernel 리팩터링 · `c11794a` AU-01 · docs(일정 변경·D5-3 기록)
Claude가 작성: `RequestMagicLink.cs` Q2 속도 제한(사용자 요청)·`HandleAsync` 리팩터링(사용자 요청), `Nook.Application.Tests` 프로젝트·핸들러 테스트, `Nook.Api.http` 요청 2개, 일정 변경 문서 반영(FE·DEP 절, 운영 반영 게이트, 배포 이후 규칙)

### 2026-10-20 (D5-4)
1. learning-log D5-1~3 "배운 것" 채움(Claude가 세션 기록으로 정리, 사용자 요청 — 결정·질문으로 얻은 것·핵심 개념 3칸)
2. 사용자 질문: A1 구현 위치 → 아직 없음(DEP 5일째 결정). 커밋 `3b7169c` 메시지가 구현된 것처럼 읽힘 — 사용자가 수정 보류
3. AU-02 설계 결정(아래 "확정된 결정 (D5)" AU-02): 트랜잭션 경계 (가) 핸들러, 응답 코드(무효 링크 400 — 처음 답 401은 D2-1 `ErrorType`과 프론트 401 공통 처리와 충돌, 닉네임 필요 200 + needsNickname), 닉네임 검사 위치. 사용자 첫 답 "Q1 전 검사 + 기존 회원은 `From`으로" → 질문 2개(Q1 전에 가입자를 알 수 있나 / `From`은 무엇을 검사하나)로 스스로 정정
4. 사고: VS 빠른 수정 "메서드 생성"이 `SharedKernel/Result.cs`에 Application 타입을 참조하는 `Success(ConsumeMagicLinkOutcome)`을 생성(CS0234) — 원인은 Claude Guidance의 `Result.Success(NicknameRequired)` 잘못된 표기. `git restore`로 복원. 교훈: 호출 쪽이 틀렸을 때 "메서드 생성"은 계층 맨 아래를 위에 맞춰 휘게 한다, 예상 밖 `M`은 먼저 의심
5. 핸들러 리뷰: 기존 회원 경로 없음(CS0161 예정)·`SignedIn` 앞 `return` 누락(D5-3과 같은 실수)·`GetUtcNow()` 두 번·`error != null` 중복 → 사용자 요청으로 Claude가 가드 절 구조로 수정(로그인·가입 공통 꼬리 Q3 → 커밋 → 반환)
6. 동시 가입(같은 이메일·다른 링크) (a) 500 수용. 피해: 사용자는 한 탭 오류(다른 탭 로그인됨), 데이터 손상 없음, 운영자는 500 로그. 예상(미측정) 교착 경로 기록 → D6
7. 통합 테스트 11개(실제 PostgreSQL, 운영 DI `AddInfrastructure`에서 핸들러). 첫 실행 전부 실패 → 오늘 안 바꾼 `CheckConstraintTests`까지 실패 → 공통 기반 의심 → Docker 엔진 꺼짐. 재실행 124개 통과. 예측 2/2 일치(전부 통과, `>=` 변이 시 만료 테스트만 실패)
8. 사용자 질문: 응답 accessToken을 이후 요청 `Authorization: Bearer`에 넣는가 → 맞음. 서버는 서명·iss·aud·exp만 검증(DB 안 봄), 60분, Refresh 없음, 로그아웃은 클라이언트가 버림. 프론트 보관 위치(메모리·localStorage·httpOnly 쿠키)는 FE 결정
9. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 124개 통과

커밋: `d1e3c87` learning-log · `720f388` AU-02
Claude가 작성: learning-log D5-1~3 배운 것(사용자 요청), AU-02 뼈대(`IUnitOfWork`·`EfUnitOfWork`·`IUserRepository`·`UserRepository`·Q1·Q3·`AuthErrors`·`UserErrors.NicknameTaken`·결과 타입·컨트롤러·DI), 핸들러 수정·리팩터링(사용자 요청 — 초안은 사용자), 통합 테스트 11개·`PostgresFixture.ConnectionString`. 결정은 사용자: (가)·응답 코드·닉네임 검사 위치·동시 가입 (a)

### 2026-10-21 (D5-5)
1. 세션 시작: progress·CS 진도 요약(퀴즈 건너뜀). Docker 꺼짐 확인 → 사용자가 켬
2. `WebApplicationFactory` 기반: `NookApiFactory`(Testing 환경 — user-secrets·개발 분기 제외, `UseSetting`으로 연결 문자열·테스트 키, 기록용 메일 큐, 워커용 콘솔 발송기) + `AuthorizationTests` 5 · `AuthApiTests` 8. 예측 2/3: ✗ 없는 경로 404 예상 → 실제 401(라우팅은 엔드포인트 null만 남기고 인가 미들웨어가 Fallback으로 먼저 응답), ○ null 이메일은 핸들러까지(`string?`), 전부 통과. 변이 `[AllowAnonymous]` 제거 → Consume 4개 401 예측 일치(NicknameTaken은 준비 단계에서 실패 — 실패 위치 읽기)
3. PL-01 결정(위 "확정된 결정 (D5)"). 사용자 질문: 0행 뒤 Find가 1행인 근거 → 세 조건 정리. 사용자 흐름 정리(대기는 미커밋일 때만, 3단계는 읽기) 확인
4. PL-01 핸들러: 사용자 초안 CS1503(`Result<Coordinate>` 그대로)·`Place.Create` 결과 버림·**null place INSERT(CS8604가 잡은 실제 버그)**·`retry_place` → 사용자 요청으로 Claude가 수정·리팩터링(`is { } x`, `CreatePlace` 도우미, 마지막 Find null은 예외)
5. 버그: PL-01 테스트 7개 `Coordinate_Latitude` 열 없음 → systematic-debugging. 사용자 가설 "RETURNING이 latitude만 돌려줘서" → 탐침 P1 LINQ ✅ / P2 순수 `FromSql` SELECT ❌ / P3 별칭 ✅ → 원인은 `FromSql` + `ComplexProperty`. B안(id만 받기) 사용자 선택, Claude 수정. incident는 수치가 없어 learning-log 한 줄로(사용자 결정, 001은 D6용)
6. 변이 `ON CONFLICT` 삭제 ×3회: 커밋 대기 테스트·20요청 동시 테스트 3/3 실패(23505), 롤백 테스트는 통과(충돌 행이 없음). 예측 △(롤백 테스트까지 실패 예상), "매번 실패?"는 이 환경 3/3이지만 보장 아님
7. BM 결정(위). StarRule: 사용자 작성(`StartRule` 오타 → 수정), TDD 빨간불 예측 일치(3실패, 18행) → 구현 → 초록
8. BM-01·02 뼈대(Claude): `IBookmarkRepository`(ON CONFLICT·`ExecuteDeleteAsync`, 영향 행 수), `IReviewRepository.ExistsAsync`, `IPlaceRepository.ExistsAsync`, `BookmarkErrors`, `BookmarkView`(Star 문자열 — Api가 Domain을 못 봄), `GetUserId()`, `BookmarksController`, DI. 핸들러는 `TODO(human)` — 작성 중에 중단
9. 검증(BM 뼈대 전 마지막): `dotnet build` 경고 0·오류 0, `dotnet test` 153개 통과(Domain 88·Algorithms 1·Application 7·Integration 57)

커밋: `dc67487` API 테스트 · `485133c` PL-01 · `46ffb8e` learning-log · `df3da5b` R-STAR. BM 뼈대 미커밋
Claude가 작성: `NookApiFactory`·API 테스트 13개, PL-01 뼈대(인터페이스·컨트롤러·`PlaceView`·DI)·핸들러 수정(사용자 요청)·`TryAddAsync` B안 수정(사용자 요청)·PL-01 테스트 12개, usecases 1a 문구(사용자 요청), StarRule 테스트, BM 뼈대. 사용자 작성: PL-01 핸들러 초안·`TryAddAsync` 초안·`StarState`·`StarRule`. 결정은 사용자: C′·흐름 위치·필드 미룸·검증 순서·B안·BM 1~4·폴더 `Stars`·enum 문자열

### 2026-10-22 (D5-6)
1. BM-01: 사용자 초안 🔴 3(조건 반전·괄호·`bool`에 `is { }`) → Claude 리팩터링. `Guid.Empty` → 500 유지(사용자 결정). BM-02 사용자 작성(정확)
2. `BookmarksApiTests` 9개. 예측: 동시 저장 진 쪽 0행 ○(보완: PK에서 미커밋 상대 대기), 변이 `bookmarked: changed` → 2개 실패 ○
3. LK-01 설계: 사용자가 "리뷰는 삭제 유스케이스가 있다"를 짚음 → Q1 ON CONFLICT는 FK 위반을 안 받아 500 ○, Q2 404 ○ → A~D 비교 → B 선택 → B의 대가(25P02·회원 FK·DELETE 신호 없음·반환 타입) 듣고 **A로 변경**, 취소 404, BM 유지
4. LK-01 뼈대(Claude) → AddLike 사용자 초안 🔴 `ReviewGone`을 `AlreadyLiked`와 묶어 200(사용자 수정) → Claude 리팩터링(switch 식·`UnreachableException`). RemoveLike 사용자 작성, 틈 사이 삭제 200 허용(사용자 결정)
5. `LikesApiTests` 10개(미커밋 리뷰 DELETE로 틈 재현, commit/rollback). 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 172개 통과(Domain 88·Algorithms 1·Application 7·Integration 76)
6. 변이 실험(catch 제거) 예측은 다음 세션으로

커밋: `bc95bd2` BM · `9c329c6`·`85f6070` docs · `52481f0` LK-01
Claude가 작성: BM-01·AddLike 리팩터링(사용자 요청), LK 뼈대(`ILikeRepository`·`AddLikeOutcome`·`LikeRepository` 23503 번역·`LikeErrors`·`LikeView`·`IReviewRepository.ExistsAsync(ReviewId)`·`LikesController`·DI), 테스트 19개, learning-log D5-5·D5-6 정리(사용자 요청). 사용자 작성: BM-01 초안·BM-02·AddLike 초안·RemoveLike. 결정은 사용자: `Guid.Empty` 500, LK A안·취소 404·틈 사이 취소 200·BM 유지

### 2026-10-23 (D5-7)
1. LK-01 변이 실험(catch 제거): 단독 실행 5/20 거짓 통과 발견 → 원인 계측(첫 요청 준비 비용으로 `ExistsAsync` ~400ms, 500ms 시간 기반 단언과 경합) → `LockWait`(`pg_blocking_pids`로 잠금 대기 직접 관찰, B안 사용자 결정) → 0/20. `ConfirmPlaceTests`도 이전
2. incident 001 작성(Claude, 사용자 요청 — 짧은 문장 + Mermaid). D6 게이트 "incident 1건 이상"으로 변경(사용자 결정)
3. RV-01~03 유스케이스 상세화: RV-01 Claude 수정, RV-02·03 사용자 작성 + Claude 보완. ADR-009(Claude 작성): 이미지 커밋 후 삭제 + 배치(D3 결정 유지, N=6h), RV-01 201, RV-03 C안(한 문장 DELETE + 영향 행 수, 없음=성공 `changed=false` `star=null`, 남의 것=403), 403은 RV-02·03 공통. 이미지 5MB·jpg/png/webp 유지
4. RV-01: 뼈대(Claude) → 핸들러 사용자 초안 🔴 3(장소 조건 반전·"지금" 세 번+로컬 시각·검증과 업로드 섞임) → 사용자 일부 수정 후 Claude 수정(사용자 요청). 5a 업로드 실패 = 500(문서 반영). 테스트 6개, 변이 2개(로컬 날짜·섞인 업로드) 모두 빨간불. 6a는 `RendezvousStorage`로 경로 고정(10/10). 고정 시계로 토큰이 만료되던 문제 → 토큰만 실제 시계로 발급
5. 예측: 정상 202(✗ 201), 4장 스토리지 1개(✗ 0개 — 장수 검사가 형식 판별보다 앞), 6a 스토리지 개수(비결정 → 고정 필요)
6. D5 범위 점검: GET이 하나도 없음 → **BM-03·PL-02를 D5에 추가**(사용자 결정, PL-02 N+1은 D9에서 측정)
7. RV-02: 순서 계약 "KeepImageIds 순서 → 새 이미지 뒤"(원본 UI도 추가·삭제만, 사용자 결정). `Review.Edit` 사용자 작성 🔴 3(새 이미지 미추가·순서 미반영·중복 ID) → 사용자가 추가·재번호 작성, 중복·순서·사전 생성은 Claude 수정(사용자 요청). 재번호가 `_images` 물리 순서를 쓰던 문제 발견(EF 복원 순서 무보장). 단위 테스트 6개 예측 전부 적중, 변이(사전 생성 제거) → 실패 후 상태 단언이 잡음
8. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 184개 통과(Domain 94·Algorithms 1·Application 7·Integration 82) — RV-02 핸들러 착수 전 기준

커밋: `b462fb2`·`4ed8507` LockWait · `36ce64d` incident 001 · `c95c0b1`·`2d5ace7` 유스케이스·ADR-009 · `e239562` RV-01 · `e3064cc` progress
Claude가 작성: `LockWait`, incident 001, RV-01 유스케이스 수정·RV-02·03 보완, ADR-009, RV-01 뼈대·핸들러 수정·테스트 6개, `Review.Edit` 수정(중복·순서·사전 생성)·단위 테스트 6개, RV-02 뼈대(`ReviewImageUploads` 추출 포함). 사용자 작성: RV-02·03 유스케이스 초안, RV-01 핸들러 초안, `Review.Edit` 초안. 결정은 사용자: LockWait B안, D6 게이트, RV-02·03·BM-03·PL-02 D5 포함, 정리 방식 D3 유지, 403, 201, C안·`star=null`, 순서 계약

### 2026-10-24~25 (D5-8)
1. RV-02 핸들러: 사용자 초안 🔴 3(작성자 확인 없음 · 업로드가 조회보다 먼저 · 커밋 후 삭제 예외 미처리) → Claude 리팩터링(사용자 요청). 순서 "내용·이미지 검증 → 조회·4b → 키 생성 → `Edit`(메모리) → 업로드 → 저장 → 삭제"로 거절은 모두 업로드 전. `ReviewImageUploads`를 `NewKeys`/`UploadAsync`로 분리, 커밋 후 삭제는 `CancellationToken.None` + 경고 로그
2. 동시 수정 진 쪽을 404와 구분 → **409 `Review.EditConflict`**(사용자 결정 (b)), usecases RV-02 6c 추가
3. **incident 002**: 동시 추가 테스트 3/10 → 40P01. psql 실험 A·B·C(사용자 실행)로 DEFERRED 재검사 ↔ reviews 행 잠금 순환 확인 → **행 잠금 `FOR UPDATE`**(사용자 결정 ①) → 0/20이 됐지만 섞인 저장(둘 다 200, `[n1@1, n2@2]`) 5/20 발견 → **`reviews.version` 낙관적 동시성 토큰**(사용자 결정 (나), `AddReviewVersion` 마이그레이션) → 0/30. 토큰 제거 변이 6/15
4. 응답 이미지 `imageKeys` → **`images: [{ id, key }]`**(사용자 결정 (A), ADR-009 결정 2 개정). 클라이언트가 `KeepImageIds`를 알 길이 없던 계약 빈틈 — 테스트가 DB에서 ID를 읽어 가리고 있었다
5. RV-03: 데이터 변경 CTE 한 문장으로 삭제 + CASCADE 전 이미지 키 회수(Claude), 핸들러는 사용자(🔴 `hasReview=false` 고정 → 수정). `ReviewImageCleanup` 공용화. 테스트 9개, 변이 3개 모두 빨간불, 동시 삭제 10/10
6. BM-03 `GET /me/places`: `PlaceView + star`, 상한 없음(D8 측정 후), 쿼리 한 번 `IN (bookmarks UNION reviews)` + EXISTS 2개(Claude, 사용자 선택). 변이로 "UNION이 중복 제거" 주석이 틀림을 발견 → `IN`(세미 조인)이 보장
7. PL-02 `GET /places/{id}` + `GET /places/{id}/reviews`: 좋아요 수·내 좋아요·작성자·`mine`, **무한 스크롤 = 불투명 커서 + 키셋 `(created_at, id)`**, 기본 10·최대 30, `limit+1`로 다음 페이지 판단(사용자 결정, Claude 작성). 동점 경계·스크롤 중 새 글 테스트, 변이 3개 빨간불
8. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 217개 통과(Domain 95·Algorithms 1·Application 7·Integration 114)

커밋: `2e9b825` RV-02 · `af623d9` incident 002 · `3aa20c7` 이미지 ID · `cc242c7` RV-03 · `3a4d2df` BM-03 · `a3fe19c` PL-02 · (이 기록) docs
Claude가 작성: RV-02 핸들러 리팩터링·409·행 잠금·`Review.Version`·마이그레이션·RV-02 테스트 10개, incident 002, 이미지 ID 응답·ADR-009 개정, RV-03 뼈대·CTE·`ReviewImageCleanup`·테스트 9개, BM-03·PL-02 전체·테스트, learning-log D5-7·D5-8(사용자 요청). 사용자 작성: RV-02 핸들러 초안, RV-03 핸들러, psql 실험 A·B·C. 결정은 사용자: 409 구분 (b), 행 잠금 ①, 버전 토큰 (나), 이미지 응답 (A), incident 미결 D6, BM-03 응답·상한·작성자, PL-02 포함 필드·무한 스크롤

### 2026-10-25 (D6-1)
1. PR #7(D5) 병합 후 `feat/d6-concurrency`. **GR-01 동시 합류 재현**: 빈자리 3에 5명 동시 → 5명 모두 성공, 멤버 6명(사용자 예측 "3 + 동시 요청 수"는 만든 회원 1명을 빠뜨림)
2. 해결책 비교 (가) `FOR UPDATE` / (나) 조건부 UPDATE(카운터 컬럼) / (다) SERIALIZABLE + 재시도 → 사용자가 (나)를 골랐다가 erd.md의 "카운터 컬럼 없음" 근거를 다시 보고 **(가) 채택**. 조건부 INSERT + COUNT는 직렬화되지 않음, SERIALIZABLE은 재시도 없이 40001(GroupFull 아님)
3. **incident 003**: 실험 1(`FromSql ... FOR UPDATE` 한 문장)이 여전히 6명 — EF가 OwnsMany 때문에 서브쿼리 + `LEFT JOIN group_members`로 감싸고, 잠금 대기 뒤 최신으로 다시 보는 것은 잠긴 groups 행뿐(JOIN한 멤버는 옛 스냅샷). **잠그는 문장과 읽는 문장을 나눠** 5/5 → 성공 3·실패 2·4명. `FOR UPDATE` 제거 변이 3/3 빨간불. ADR-010
4. **GR-02 생성**: 그룹 + 첫 멤버는 `SaveChanges` 한 번(암묵적 트랜잭션) → 없는 회원으로 멤버 INSERT만 FK 실패시켜 그룹도 안 남음 확인(사용자 예측 적중). 초대 코드 충돌은 `TryAddAsync` → `false`로 최대 3회 재시도, 다 실패하면 예외(사용자 결정). 사용자 루프 초안(`catch (Exception)` 재시도·반환값 무시·범위 오류) → Claude 수정(사용자 요청)
5. **R-GR-02·04**: 같은 회원의 합류·생성이 서로 다른 그룹이면 그룹 행 잠금으로 못 막음(사용자 답) → 공통 행 = users 행. **잠금 순서 users → groups, 강도 `FOR NO KEY UPDATE`**(사용자 결정 — FK 검사 `FOR KEY SHARE`는 통과, 자기끼리는 충돌). 재현: 참여 2개 회원이 합류 2 + 생성 1 동시 → 4개 → 사용자 구현(`>` 경계 오류 한 번) 후 5/5 성공 1·실패 2·3개. 합류 잠금 제거 변이 → 5개(생성의 잠금만으로는 못 막음 — 같은 잠금을 쓰는 쪽끼리만 줄 선다). 반대 순서 교착 학습 테스트(40P01, 약 1초), 강도 변이(`FOR UPDATE` → FK INSERT 시간 초과)
6. 생성의 코드 재시도를 트랜잭션 안으로 — UNIQUE 위반 후에도 계속 쓸 수 있는 것은 EF가 `SaveChanges`마다 세이브포인트를 만들기 때문(충돌 테스트 통과로 확인)
7. **R-GR-09 탈퇴(GR-04)**: 공통 행 = groups 행(사용자 답), users는 안 잠금(탈퇴는 R-GR-02를 못 깸). `LockByIdAsync`(문장 분리) + `DeleteAsync`. 사용자가 `MemberRemoval` switch + `UnreachableException` 작성. 마지막 두 멤버 동시 탈퇴 10라운드 × 3회 그룹 0, 잠금 제거 변이 10/10 빈 그룹. 탈퇴·합류 겹침은 "탈퇴 먼저" 9~10/10 — 합류는 users 잠금 왕복이 하나 더 있어 늦게 도착(선착순 = 잠금 획득 순)
8. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 239개 통과(Domain 95·Algorithms 1·Application 7·Integration 136)

커밋: `339d328` GR-01 · `79a1efd` incident 003·ADR-010 · `5992212` GR-02 · `271f70f` R-GR-02/04 · `5879f1b` GR-04
Claude가 작성(D6-1): 모든 테스트(Join·Create·GroupMembershipLimit·Leave)와 변이 실험, `GroupRepository` 전체(`LockByInviteCodeAsync` 문장 분리·`LockByIdAsync`·`TryAddAsync`·`CountMembershipsAsync`·`DeleteAsync`), `UserRepository.LockAsync`, `IInviteCodeGenerator`·`RandomInviteCodeGenerator`·`GroupView`, GR-02 핸들러(재시도 루프 수정 포함, 사용자 요청)·잠금 부분, 합류 핸들러 AlreadyMember 순서 변경, 도메인 `Group.MaxGroupsPerUser`·`GroupErrors.TooManyGroups`, incident 003·ADR-010·erd.md groups 표기(사용자 요청).

### 2026-10-26 (D6-2)
1. **incident 002 미결 ①**: 원인 = 랑데부는 출발선(둘 다 조회 완료)만 고정, 커밋 순서는 스케줄러. 사용자 예측 △ — 반대 순서(keep A 먼저)에서 진 쪽 B 문장을 INSERT로 답함(실제: 추적 중 엔티티의 `ChangeSortOrder` → `UPDATE sort_order` 0행), 0행 결과는 정답
2. `OrderedGateStorage`: Barrier 랑데부 뒤 한쪽 `PutAsync`를 `TaskCompletionSource`로 붙잡고, 테스트가 이긴 쪽 200(= 커밋 완료)을 받은 뒤 푼다. `Edit_ConcurrentWithDifferentKeep_LaterSaveReturns409` Theory(keep B·C 먼저 = version 경로 / keep A 먼저 = 0행 경로). 10/10 초록
3. 변이(`IsConcurrencyToken()` 제거, 사용자 실행): `True`만 매번 빨강(`later` 200) — version이 꼭 필요한 순서는 하나뿐임을 확인. 첫 실행 실패는 테스트 결함(응답 본문 두 번 읽기 → `ObjectDisposedException`)
4. **incident 002 미결 ②**: 사용자 예측 △ — "화면 버전이 낮으니 실패"(실제: 화면 버전이 서버에 안 가므로 41줄이 최신을 읽고 통과 → 덮어씀). 결정(사용자) A 토큰 `reviews.version` · B 바디 필드 · C 409 재사용 · D 필수. 변경 비용: A는 불투명 값 계약이면 서버 내부, B는 단건 GET 신설 + FE 흐름 변경
5. 구현: 사용자가 핸들러 사전 비교 작성(권한 뒤·업로드 전 — 남의 리뷰에 409면 정보 노출). 41줄 비교 통과 시 읽은 값 = 화면 값이라 저장의 version WHERE가 그대로 최종 판정(41 vs 58 양자택일은 Claude 질문 오류)
6. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 242개 통과(Domain 95·Algorithms 1·Application 7·Integration 139)
7. **그룹 API**(사용자 결정, Claude 추천 채택): 합류는 코드만 아는 상태라 `/groups/{id}/membership`에 PUT할 수 없음 → `POST /groups/join`, 코드는 본문(경로는 로그에 남음). 합류 응답 `200 + GroupView`(핸들러 `Result<GroupView>`로 변경), 탈퇴 `DELETE …/membership` 204. 그룹 핸들러가 API DI에 빠져 있던 것도 등록. 첫 실행 4건 실패 = 시드 시각이 실제 시계보다 미래(`AddMember`의 joinedAt ≥ CreatedAt 가드)
8. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 252개 통과(Domain 95·Algorithms 1·Application 7·Integration 149)

Claude가 작성(D6-2): `OrderedGateStorage`·Theory 테스트, incident 002 §6·§7 갱신, Version 뼈대(View·Request·Command·컨트롤러)와 테스트 2개·헬퍼, ADR-011·usecases.md·erd.md·ADR-009 갱신(사용자 요청), `GroupsController`·DI 등록·합류 핸들러 반환 타입·`GroupsApiTests` 10개·기존 그룹 테스트 헬퍼 수정. 사용자: 변이 실행·되돌리기, 버전 사전 비교, 결정 A~D. 사용자 작성: GR-01 핸들러, 합류 users 잠금·3개 검사, 탈퇴 switch. 결정은 사용자: (가) FOR UPDATE, 코드 충돌 3회 후 예외, 생성 응답 `GroupView`, 잠금 순서 users → groups, `FOR NO KEY UPDATE`, AlreadyMember 우선, 없는 그룹 탈퇴는 성공(멱등)

### 2026-10-26 (D6-3)
1. 이월 메모(그룹 조회 API) 원본 조사: 그룹 함수 6개 중 `getMyGroups`만 유스케이스 목록에 없음(MapScreen 첫 로드가 호출). D1 목록이 쓰기 위주로 뽑혀 "화면 진입용 읽기"가 빠짐. 판단은 PM 확인 후
2. 회원 탈퇴 users 잠금(D6 미결): PF-02가 일정에 없음 → PF-02 구현 때로 미룸(사용자 결정). 흩어진 PF-02 미결 3건을 "PF-02 착수 전 결정 묶음"으로 합침
3. `progress.md`에 "PM 확인 항목" 절 신설(프론트 범위·그룹 조회·PF-02 범위·R-RV-03 재작성)
4. 검증: `dotnet build` 경고 0·오류 0, `dotnet test` 252개 통과(Domain 95·Algorithms 1·Application 7·Integration 149)

Claude가 작성(D6-3): 원본 조사 표, `progress.md` 정리(PM 확인 항목·PF-02 묶음·낡은 줄) — 사용자 요청. 결정은 사용자: PF-02 미룸, PM 확인 대상

### 2026-10-26 (FE-1)
1. 결정(사용자): 같은 리포 `web/`, TS + Next.js 그대로, RN 변환 안 함(사용자 개인 의견이라 PM 항목에도 넣지 않음)
2. 원본 함수 29개 ↔ API 대조: 바로 쓸 수 있는 것 13개, 데모를 막는 빈 곳 3개(`GET /me`·단건 리뷰·이미지 표시) + 토큰 저장 위치. 그룹 조회와 같은 원인 — 쓰기 위주로 만든 백엔드에 화면 진입용 읽기가 빠짐
3. 가져오기: `git archive 668390e`로 추적 파일만(`.env.local`·`node_modules`·미커밋 변경 차단), 70개. 원본 CRLF → `.gitattributes`로 LF 저장. 사용자가 화면 확인 후 커밋(사용자 요청)
4. 사고(예방): 원본 `.env.local`이 `web/`이 아니라 리포 루트로 복사됨 — `web/.gitignore`는 `web/` 안만 막아 루트에선 미추적·비무시 → `web/`로 옮기고 루트에도 `.env*.local`
5. `npm audit`: critical 1(next 14.2.35, 수정판 16.x) · high 2(nanoid·source-map-js, `audit fix` 가능)
6. `GET /me` 설계 질문: ① 원본 3상태 → 사용자 답 `401 / 401 / 200` — 결과는 맞음. 단 `no_profile`은 우리 구조에 없음(닉네임 없으면 consume이 토큰을 안 줌 → 토큰 없는 401). 닉네임 화면 분기는 consume 응답으로 ② 서명 유효 + 회원 없음 → 사용자 첫 답 "다른 회원 ID로 바꾼 요청, 400" — 정정: `sub`를 바꾸면 서명 검증에서 미들웨어 401, 핸들러에 오지 않음. 실제 경우는 탈퇴 후 남은 토큰·개발 중 DB 초기화. 400은 형식 오류라 부적합 → **결정(사용자): 401**("로그인 화면으로 보내야 하니까"). Claude가 앞서 적은 "404 권장"은 철회

커밋: `73a14db` 가져오기 · `508de04` 루트 gitignore · `a41e2d0` progress · `48bb561` audit fix (`feat/fe-connect`). 6번(`GET /me` 결정)은 progress.md 미커밋
Claude가 작성(FE-1): 원본 대조 표, `web/` 가져오기, `.gitignore`, `progress.md` — 사용자 요청. 결정은 사용자

### 2026-10-27 (FE-2)
1. `GET /me`(AU-04): `ErrorType.Unauthorized` 추가(D2-1 "401은 미들웨어 몫" 번복 — ErrorOr 등 Result 라이브러리 관례, 사용자 요청 "대중적인 방법"). 미들웨어 401은 title 없음, 핸들러 401은 `Auth.UnknownUser`. 변이(null 검사 제거) 예측: 사용자 "두 테스트 깨짐" → 실제 1개(회원 있는 테스트는 그 줄을 안 지남)
2. 리팩터링: `ErrorResults.Problem(this ControllerBase, Error)` 한 곳으로(복사된 표 4개 + 하드코딩 400/404 제거). 기본 분기 `throw`로 통일(Groups·Reviews는 조용한 500이었음). 테스트 무수정 255개 초록. 확인 질문(표 누락 시) ○
3. `GET /reviews/{id}`(RV-04): A안 작성자만 + 403(사용자 결정). 원본은 누구나 읽고 클라이언트가 본인 확인 → 서버 계약으로. version 왕복 테스트로 RV-04→RV-02 계약 고정
4. PM 결정(다른 세션) 반영: 범위 = 원본 화면 전체, PF-02 ⓪ (가) users DELETE + `SET NULL`. 사용자 지적으로 "할 일 목록이 데모 범위 그대로" 발견 → `docs/design/fe-backend-backlog.md` 작성(원본 함수 28개 대조: 있음 17 · 보강 2 · 새 API 9 · API 밖 결정 2)
5. 범위 답(사용자·PM): 아바타 = 프로필 사진(직접 설정, 없으면 기본, 가입 직후 PF-01로) · 성별·생일 가입 때 입력, 생일 `date`, FE가 `YYYY-MM-DD`로 변환 · 그룹 색 = 디자인 확정 전 → **슬롯 번호(0~3) 저장** 제안, 그룹맵 표시 정책(PM: 리뷰 중첩 = 첫 리뷰어 색 + 리뷰한 멤버 수, 북마크만 중첩 = #4A4A4A + 수, 단독 북마크 = 회색 + 멤버 색 외곽선) → 응답은 장소마다 1행·사실만, 그리기는 FE. 가입 전 리뷰 포함
6. 결정: 이미지 A(로컬 디스크 + 익명 `GET /images/{key}`, 서버가 `Url` 생성, DEP에서 S3로 구현체 교체) · 토큰 ③ BFF · 로그인/가입 문구 통일 (a) · 토큰 60분 유지
7. `IsBookmarked`(PL-02): 별은 리뷰가 있으면 fill이라 북마크 상태가 가려짐(정보 손실). 이미 조회하던 값을 응답에 추가, 쿼리 수 그대로
8. 가입 성별·생일(R-US-04·05): `Gender` enum 1부터(default 차단), `Birthdate`(1900-01-01~오늘 UTC), 새 회원만 검사, 마이그레이션은 기존 행 `'none'` 채운 뒤 기본값 삭제, CHECK에 `current_date` 안 씀(비결정적). 변이(상한 제거) → 도메인·통합 각 1개 빨간불

9. FE 인증 교체(BFF): 로그인·가입 문구 통일 (a) · 성별·생일 백엔드 먼저 · 토큰 60분 유지(사용자 결정). 링크 토큰이 `#` 프래그먼트라 도착 지점은 route handler가 아니라 클라이언트 페이지. 가입 대기 쿠키가 원본 `no_profile` 자리를 대신해 3상태 가드 재사용. 가입 폼 표시용으로 consume `needsNickname` 응답에 이메일 추가
10. BFF 중계: 사용자 요청 "안전하고 대중적인 방식" → 범용 중계 route handler + 허용 목록 추천·채택(Server Actions는 본문 1MB 기본 제한·읽기 용도에 안 맞음). open proxy 방지 장치 4개. 토글 → 목표 상태 명시(PUT/DELETE)
11. 수동 확인 중 오진 1건: 401 원인을 "이메일당 메일 제한"으로 설명 → DB 확인 결과 2건뿐이라 틀림 → 실제는 테스트 스크립트(`grep -c` 0 → 종료 코드 1 → `&&` 중단). 측정 전 추측을 원인으로 말하지 않기
12. 이미지 표시 A안: 키 모양 검사(`ReviewImageKeys.IsValid`) + 디스크 root 밖 거절(2겹). 변이로 모양 검사를 지우면 모양 사례 3개만 빨간불 — `..` 사례는 URL 정규화가 먼저 막아 우리 검사를 지키지 않음을 확인하고 테스트 주석에 남김
13. 리뷰 흐름: `ReviewEditor`가 사진을 고르는 즉시 올리던 것(원본 고아 이미지 원인) → 파일을 들고 있다 저장 때 한 요청으로. 목록이 페이지가 되면서 "내 리뷰가 있나"를 목록으로 판정할 수 없게 됨 → 별은 서버 판정(PL-02 상세)으로. 무한 스크롤 (b)는 사용자 결정
14. R-RV-03 하루 기준을 KST 00시로(사용자 결정, D2-2 UTC 번복). 화면 문구 "00시 기준"과 서버 실제 기준(KST 09시)이 달랐던 것을 리뷰 연결 중 발견. 비용이 작았던 이유: `review_date`는 앱 계산 컬럼이고 `created_at`과 묶는 CHECK를 D3에서 일부러 두지 않았다. 운영 데이터 전이라 재계산 충돌(UTC 다른 날 → KST 같은 날 = UNIQUE 위반) 위험도 없었다. 테스트 먼저(빨간불 확인) → `ReviewDay` 한 곳
15. 프로필: PF-04 키셋 재사용, PF-03 API 없음, PF-01 닉네임 미리 확인 없음(원본 화면이 직접 안 불렀다)·사진 `avatars/`. 정정 2건: "원본에 사진 삭제 버튼 있다"(없음), 함수 수 15 → 16
16. 그룹: 방장(PM 허가, ADR-012 — ADR-004 결정 6 번복, 원본 CASCADE 결함 대신 넘기기 + RESTRICT), 색은 슬롯 저장(디자인 미정), N+1 → SQL 1문장(로그로 확인, 비교 측정은 D9), 그룹 지도는 장소마다 1행 사실(사용자 A안). owner FK 때문에 원자성 테스트가 다른 경로를 타게 된 것을 발견 → 임시 CHECK로 원래 상황 재현
17. 탈퇴(PF-02): PM 답 — 복구 없음, 좋아요 유지. 좋아요의 복합 PK `(user_id, review_id)`는 NULL이 될 수 없어 대리 키 + UNIQUE(A안, 사용자 결정) — "규칙이 PK 자체"(D2)의 대가. 남은 토큰은 요청마다 회원 조회 대신 FK 위반을 401로 번역(DB가 최종 판정자). 변이로 발견: 그룹 나가기를 빼먹은 **버그가 401로 가려짐** → 자식 쪽 위반("insert or update")만 401, 부모 쪽은 500. `UseExceptionHandler` 추가로 처리 안 된 예외는 500 ProblemDetails
18. Supabase 제거: 화면 코드에 참조 0. npm audit은 제거 전후 같음(next critical 1, glob high 3 — dev lint 경유)
19. (2026-10-28) 브라우저 확인 시작 — 로그인·지도 정상. 사진 저장 위치 질문(로컬 디스크 `App_Data/images`, DB엔 키만). 로컬 DB에 Supabase 데이터가 넘어오지 않았음을 확인(회원 10명 출처 대조). **버그 보고 1건**(그룹 지도에서 다른 멤버의 북마크만 장소가 안 보임) → 가설 H1~H3, 재현 순서 정리, 원인 미확정 — 다음 세션 재개 지점
커밋: `0ed2936` /me · `7a3aba2` ErrorResults · `19aca13` RV-04 · `81d42bd`·`4ad7883` 백로그 · `a0d5ac3` IsBookmarked · `40277eb` 링크 포트 · `e8f1590` 성별·생일 · `52bf45f` progress · `5a36a5d` 가입 이메일 · `47e8ffd` BFF 인증 · `06dd87d` BFF 중계·지도 · `805cb16` progress · `7f93f8f` 이미지 표시 · `863cea1` 리뷰 흐름 · `e663bfb` KST 하루 · `3a531ad` progress · `b1a6e79` PF-04·03 · `a5a6f0e` PF-01 · `ed28fbc` 방장·슬롯 · `ea94b3f` GR-03·06 · `1f13faa` progress · `237d1f8` GR-05 · `403bdd6` FE 그룹 · `cb31f42` progress · `9d4440e`·`33f84cd`·`2683756` 탈퇴 · `795dbb3` Supabase 제거 · 이 progress 커밋 (`feat/fe-connect`, 미푸시)
Claude가 작성(FE-2, 사용자 요청): ADR-012, 탈퇴 전체(스키마·절차·예외 처리기·FE), 그룹·프로필 전체, web/ BFF 전부(route handler·중계·`store.ts` 교체·화면 수정), AU-04·RV-04·R-US-04·05 유스케이스 줄과 규칙, `GetMe`·`GetReviewForEdit` 핸들러 본문(`GetMe`는 사용자 작성), `ErrorResults`, 백로그 문서, `Gender`·`Birthdate`·`User` 변경·마이그레이션·erd.md(도메인·스키마도 위임), 테스트 전부. 결정은 사용자·PM

### 2026-11-05 (DEP-1)
1. 세션 시작: FE 완료 확인 → DEP 진행. 사용자 요구 — 언어·구동 방식·아키텍처에 맞게, **보안 1순위·비용 2순위**, AWS EC2, CI/CD는 GitHub Actions 권장, AWS 보안 Pillar 5개(IAM·Detection·Infrastructure protection·Data protection·Incident response) 검토
2. 계획 10단계 + Pillar 점검표 → `docs/design/dep-plan.md`(지난번 대화에만 있던 목록을 잃은 일이 있어 파일로 남김)
3. 결정(사용자): 프론트 같은 EC2 · DB 컨테이너 · 사진 S3 · GHCR→ECR · SSH→SSM · 도메인 구매. 사용자 질문 "ECR의 이미지 = 리뷰 사진?" → 아님(컨테이너 이미지 = 코드, 업로드 사진 = 데이터. 수명 주기가 반대)
4. 도메인 없음의 영향: HTTPS(쿠키 `Secure`)·SES DKIM/DMARC(매직 링크 도착)·카카오 도메인 등록 → A안 구매
5. 1단계 Next 14.2.35 → 16.4.5: Promise 요청 API 6곳, `middleware`→`proxy`, ESLint flat, 운영 취약점 0. 막은 critical 중 하나가 "Image Optimization API SVG RCE"인데 `remotePatterns: "**"`로 열려 있었음 → 사용처(온보딩 로컬 3장)만 남기고 닫음, 실측 200/400/400
6. 발견(2단계 함정): BFF Origin 검사가 프록시 뒤에서 모든 POST를 403으로 만듦 / ForwardedHeaders 없으면 IP RateLimiter가 전원을 한 명으로 봄
커밋: 없음(`fix/next16-security` 작업 트리 — 다음 세션 첫 결정)
Claude가 작성(사용자 요청): dep-plan.md, web/ 업그레이드 전부, `next.config.js` 이미지 설정(사용자: "Next.js는 아직 학습 전"), progress·learning-log 갱신. 결정은 사용자

### 2026-11-09 밤 (DEP — 도메인·SES·5단계 준비)
1. 도메인 `fillyournookwithstars.com`(PM 결정, Route 53 $16/년) 구매 · 이메일 인증 · 요청 성공·호스팅 영역 확인. `.com`이라 HSTS 프리로드 없음 → 5단계 HTTPS·HSTS 직접 확인
2. SES 서울 도메인 인증(Easy DKIM 2048, MAIL FROM 끔 — DMARC는 DKIM 정렬로 충분) → 샌드박스 테스트 Gmail 받은편지함·SPF/DKIM/DMARC PASS → DMARC `none` → `quarantine`(공개 DNS 실측) → 운영 승인 요청 제출(결과 대기). 사용자 질문: SES가 뭔가, 샌드박스면 가입자에게 무슨 일(→ 발송 거절 + 워커라 화면은 성공)
3. **사고(경미)**: 테스트용 이메일 자격 증명을 시드니에 생성 — 콘솔이 마지막 리전 기억. 삭제·서울 재생성 → 시드니 자원 0개 조회 후 **SCP `RegionFloor`·표시 리전에서 시드니 제거**, 시드니 호출 = SCP 명시적 거부 실측(사람 주의 → 구조적 차단)
4. 카카오 JS SDK 도메인 추가 · `nook-readonly` → `ViewOnlyAccess + SecurityAudit`(PR #42·#43)
5. 5단계 A·B: S3 2개 **계정 리전 네임스페이스**(새 콘솔 옵션 — 화면 문구 먼저 받아 판단, 무작위 접미사 폐기), 이미지 수명 주기 35일, 백업 버전 관리(Claude가 "끔" 권장 → 침해 시나리오로 정정) · 역할 `nook-ec2` + 인라인 정책(**백업 문 사용자 작성**: `s3:PostObject`(없는 액션)·복사된 접두사 2곳 리뷰로 수정 → `daily/`·`weekly/` Put만). Claude 실수: `<ACCOUNT_ID>` 개수 5 → 사용자가 6으로 정정
6. EC2는 6단계(Parameter Store) 뒤로(사용자 결정 — 빈 인스턴스 과금·노출 기간 단축)
커밋: PR #42·#43 병합, `dbd723e`(`feat/dep5-infra`, 푸시·PR 전)
Claude가 작성: 정책 틀(백업 문 제외), 문서 갱신. 콘솔 작업·결정은 사용자

### 2026-11-09 저녁 (DEP 7 §5 — 개인정보)
1. 발견: 성별(필수)·생년월일(선택)을 가입 때 받아 **저장만 하고 어디서도 쓰지 않음**(프로필도 `gender: null`) → 최소 수집 원칙과 충돌
2. 결정(사용자, PM 확인 대기): 둘 다 선택 + 별도 동의 · 만 14세 미만 가입 불가(자기 신고 필수 + 생년월일 있으면 서버 거절) · 탈퇴 후 리뷰 유지 + 안내 · 접속 로그 30일
3. 구현(PR #40): `PrivacyConsent` + R-US-06, 방침 버전은 서버 값, 마이그레이션 `AddPrivacyConsent`(`ck_users_optional_consent` NOT VALID — compose의 옛 행 1개로 실측, Down의 EF 기본값 `""` CHECK 위반 수정), 가입 폼 체크 3개, `/privacy` 초안, 설정 링크. **탈퇴 확인 문구가 사실과 달랐음**("모든 데이터 삭제" — 리뷰는 남음) → 정정. 변이 2개(필수 동의·나이 검사) 빨간불. 브라우저 확인(사용자) + DB 기록 확인
4. 사용자 질문: 성별·생일을 꼭 받아야 하면 필수로? → 기술적으론 몇 줄, 법적으론 "없으면 서비스 불가"인 목적이 있어야(제16조 최소 수집·거부권). 통계·추천·마케팅은 보통 선택. PM에게 "어떤 기능에 쓰나"부터
커밋: `eae1bc4`·`2c92a31`(#40)
Claude가 작성(위임 기간): 도메인·스키마·마이그레이션·API·FE·테스트, 처리방침 초안(`[PM]` 표시). 결정은 사용자, 원문 확정은 PM

### 2026-11-09 오후 (DEP — 도메인 무관 작업 마무리)
1. 문서 PR #36: progress·learning-log·**ADR-013**(클라이언트 IP 신뢰 고리와 요청 제한 위치, Claude 작성 — 사용자 요청)
2. BFF 인증 경로(PR #37): Origin 검사가 빠진 곳이 메모(3곳)보다 많았음 — **consume = 로그인 CSRF**(쿠키 불필요라 SameSite로 못 막음), magic-link = IP 제한 우회. `rejectCrossOrigin`·`problemResponse`(Retry-After) 공통화. 429 화면: 링크 열기 "잠시 후"(이전엔 "유효하지 않음" → 잘못된 재요청 유도), 가입 폼은 exhaustive switch가 누락을 컴파일 오류로 잡음. compose 실측 + 브라우저 로그인·로그아웃(사용자)
3. 백업·복원(PR #37): 결정(사용자) 하루 1회·보존 7+4. 리허설 통과, 잘린 덤프·덤프 뒤 쓰기는 실패로 잡음. 사용자 질문 "덤프 뒤 쓰기가 왜 실패?" → 원본을 지금 세는 한계(덤프 자신을 기준으로 바꾸는 안 제시, 사용자: 수정 없이 진행)
4. arm64(PR #37): 결정(사용자) 교차 빌드 + web만 QEMU → **번들 교차 빌드 실패 실측**(`Could not load file or assembly 'Nook.Api'` — 도구가 arm64 빌드를 로드) → 번들도 대상 플랫폼. 로컬 api 15s·migrate 854s·web 861s, 에뮬레이션 실행 확인. **PR CI는 x86 유지**(사용자 결정, CI images 5분 39초 — 기준선). 배포 스크립트 롤백 리허설 good → bad → good
5. 메일(PR #38): A1 측정 — 동시 100요청 → Q2 통과 8건 → 발송 3통(제거 변이 12통). SES 발송기 UTF-8 Charset 고정. 사용자 질문: SES란(발송 전용, 받은편지함 없음), "인증한 도메인의 주소"(= `no-reply@구매할도메인`, DKIM 레코드로 소유 증명)
6. 아티팩트: 변경점 그림 정리 https://claude.ai/artifact/T74t79HyxxvMhszDAndVLo (비공개)
커밋: `25e361f`·`e8254df`·`fed3b9c`·`9918032`(#37) · `e97e9f3`(#38) · 이 progress 커밋
Claude가 작성(위임 기간): 위 코드·스크립트·테스트 전부, ADR-013, dep-plan 갱신. 결정은 사용자(백업 주기·보존, ARM 빌드 방식, PR CI x86, 리허설 스크립트 수정 안 함)

### 2026-11-09 (DEP — 7단계 코드)
1. Dependabot 잔여 #17·#18: 내용이 이미 `main`(#30)이라 충돌 해결 대신 닫기 → 브랜치 삭제(사용자). 병합 버튼 비활성 = 충돌 PR 정상 동작
2. 도메인: 등록처 Route 53(사용자). `nook.com`(1997~)·`nookmap.com`(2026-04~) 등록됨 — Verisign RDAP로 AWS 로그인 없이 조회. 후보 `nookmap.app`. **PM 부재로 이름 결정 보류** → 도메인 무관 작업 먼저(A안, 과금 시작 안 함)
3. S3 이미지 저장소(PR #33): 결정 — 이름 `IImageStorage`·API 프록시 유지(버킷 비공개)·테스트 MinIO → **`adobe/s3mock`**(MinIO 공개 이미지 없음: Docker Hub 없음, quay 401). 처음부터 초록인 테스트를 변이로 점검하다 **약한 테스트 발견**: 형식 검사를 지워도 S3는 404 → null이라 통과 → "객체를 올려 둔 뒤에도 null"로 수정
4. IP 요청 제한(PR #34): 실제 경로 Caddy → web(BFF) → api(dep-plan 가정과 다름) → 고리 3개(Caddy 덮어쓰기·BFF 맨 오른쪽 전달·api 신뢰 대역). **신뢰 목록이 비면 "모두 믿음"** — 변이로 실측. 한도(사용자): 전체 300/분, 매직 링크 요청·소비 각 5/10분(칸 = IP + 경로 — 가입 1회가 3칸 쓰는 문제). 테스트 설계 실수(전체 한도를 클래스 전체에 낮춤)로 두 한도가 모두 적용됨을 확인. compose 실측: 위조 XFF 6회 → 6번째 429. 서브넷 변경 후 migrate DNS 실패 → 가설(기존 컨테이너 이어 붙이기로 `db` 별칭 빠짐) → `down`/`up`으로 확인
5. 헬스체크(PR #35): live/ready 분리·배포 스크립트가 임시 curl 컨테이너로(사용자). compose에서 db 정지 시 live 200·ready 503 실측. `IncludeErrorDetails` 운영 동작 테스트로 고정(D5 이월 해소), `UseHttpsRedirection` 제거
6. 보안 헤더·CSP(PR #35): Report-Only 측정 → SDK는 `t1.kakaocdn.net`(추정 `daumcdn` 틀림), 로컬 http 때문에 스킴 불일치 617건 → 스킴 없는 출처 → 강제 모드에서 `'unsafe-eval'` 없이 전 기능 정상(사용자 브라우저 확인)
커밋: `50431ca`·`567a885`·`fe4c337`(#33) · `c474a7a`·`17f78c6`(#34) · `398887c`·`8ab8a9d`·`119c90a`·`fc293ef`(#35) · 이 progress 커밋
Claude가 작성(서비스·테스트 위임 기간): S3 구현·테스트·픽스처, ForwardedHeaders·RateLimiter·BFF XFF 전달, 헬스체크, Caddy 헤더·CSP, dep-plan 갱신. 결정은 사용자(등록처·도메인 보류·A안·이름·프록시·테스트 대상·제한 위치·범위·수치·헬스 모양·호출 주체)

## 체크리스트

진행 순서(2026-10-20 변경): D5 → D6 → FE → DEP → README 최소판 → D7 → D8 → D9 → D10 → D11 → D13 → D14

### 1주차 — 설계와 정합성
- [x] D1 도메인 분석·유스케이스·솔루션 골격·ADR-001
- [x] D2 클래스·엔티티·값 객체 (design-review 🔴 0) — Error·Result, 값 객체 8개, 강타입 ID 5개, 분류표, 다이어그램, 엔티티 8개, Group 단위 테스트 21개, ADR-004~007
- [x] D3 ERD·스키마·EF Core 매핑·마이그레이션·ADR-002 (design-review 🔴 0, `database update` 성공) — 테이블 8개, Mermaid·보장 주체 표, compose, `Init` 마이그레이션(SQL 제약 3개), `From` 6개, ADR-002·008
- [x] D4 테스트 설계(단위 + Testcontainers 통합) — Domain 84·Integration 18, DB CHECK 11개 전부, 시각 가드(ADR-007), 맞바꾸기(DEFERRED), 리뷰 본문 정규화·`joinedAt` 결정
- [x] D5 핵심 API(장소·북마크·리뷰·좋아요·JWT) — 게이트 충족(API 테스트 정상·실패 흐름), `main` 병합은 사용자
- [x] D6 트랜잭션·동시성(그룹 합류) — PR #8 병합(2026-10-26) — incident 001 ✅·002 ✅(D5에서 선행)·003 ✅, ADR-010·011, GR-01·02·04 + R-GR-02/04 ✅(게이트 충족), incident 002 미결 2건 ✅, 그룹 API 3개 ✅. 남음: 푸시·PR·병합(사용자)
- [x] FE 프론트 연결(원본 화면 전체 — PM 2026-10-27) — 함수 28/28 ✅(BFF·지도·리뷰·프로필·그룹·탈퇴), Supabase 제거 ✅, 브라우저 확인 ✅, PR #9 병합(2026-10-28)
- [ ] DEP 배포 주간(1주) — Docker·compose·AWS EC2·Caddy HTTPS·GitHub Actions·ADR-003·공개 전 필수 보안(RateLimiter·메일 상한·실제 발송기)·백업 — 계획 `docs/design/dep-plan.md` 단계 1~10. 단계 1·2·3 ✅, 4단계 대부분(2026-11-07)
- [ ] README 최소판(D14 일부)
- [ ] D7 DSA #1 근처 장소 검색(Geohash) — dsa/01 · 🚀 운영 반영

### 2주차 — 대용량·성능·배포
- [ ] D8 대용량 시드·인덱스·Keyset(PL-02 키셋 `EXPLAIN` 포함) — incident 003~ · 🚀 운영 DB에 `CONCURRENTLY`
- [ ] D9 쿼리 튜닝·N+1·카운터 전략 · 🚀 운영 반영
- [ ] D10 k6 부하 테스트·커넥션 풀 — incident 004 · 🚀 풀 설정 반영
- [ ] D11 LRU 캐시·Token Bucket — dsa/02, 03 · 🚀 compose에 redis 추가
- ~~D12 Docker·CI/CD·HTTPS 배포·ADR-003~~ → DEP로 이동
- [ ] D13 운영 검증·장애 시나리오
- [ ] D14 README·회고

## PM 확인 항목
> 사용자 혼자 정할 수 없는 범위·정책 질문. PM과 의논한 뒤 결과를 "확정된 결정"으로 옮기고 여기서 지운다.

(2026-10-27 1~4 답변 받음 → "확정된 결정 (PM, 2026-10-27)". 아래는 PF-02 구현 전까지만 답 받으면 됨, 지금 진행엔 영향 없음)

1. **탈퇴 철회·재가입 시 예전 리뷰 복구가 없어도 되나** — PF-02 (가)안(`SET NULL`)은 복구 불가. "필요하다"면 ⓪ 재검토
2. **탈퇴 회원이 남의 리뷰에 누른 좋아요를 지우나** — 지우면 그 리뷰의 좋아요 수가 줄어듦. 북마크·그룹 멤버십 삭제와 함께 PF-02 절차에 들어감
3. **(알림) 로그인·가입 화면 안내 문구 변경** — 명세서 :81(이미 가입된 이메일)·:148(가입되지 않은 이메일) 안내를 없애고 "메일을 보냈어요"로 통일(FE-2 사용자 결정). 이유: 가입 여부를 노출하지 않는 백엔드 설계(D5). 답이 필요한 질문이 아니라 알림
4. **그룹맵 세부(GR-05 때)** — 색 팔레트는 디자인 확정 후. 그때 슬롯 → 색 매핑만 FE에서 정한다

## 확정된 결정 (PM, 2026-10-27)
1. **프론트 연결 범위 = B안, 원본 화면 전체.** "데모 밖"으로 분류했던 것도 전부 범위 안: `getMyReviews`·`getStarCounts`(PF-03)·`updateProfile`·`nicknameExists`·아바타 업로드(User에 아바타 없음 → 스키마 변경)·`deleteAccount`(PF-02)·`renameGroup`(GR-03)·`getGroupSavedPlaces`(GR-05)
2. **그룹**: 앱을 다시 열어도 "내 그룹 목록"이 보여야 함(`getMyGroups` 필요 → 사용자가 유스케이스부터) + **GR-05 그룹 멤버 저장 장소 지도 포함**. 그룹별 `memberCount` N+1을 그대로 둘지는 사용자 결정
3. **PF-02 회원 탈퇴 = 범위 안(1번 B안). 정책: 리뷰와 리뷰 이미지는 자산이라 탈퇴해도 보존.** ⚠️ 지금 설계와 충돌 — reviews가 users 삭제 시 CASCADE, erd.md 스토리지 키에 회원 ID, D6-3 정리의 "모든 데이터 삭제" 전제. 설계를 다시 해야 함(아래 "미결 결정" PF-02 묶음)
   - 추가 답변(같은 날): 탈퇴 회원 리뷰의 작성자는 **"탈퇴한 사용자"**로 표시. 단 **한 탈퇴 회원의 리뷰끼리 묶어 볼 수 있으면 안 됨**(연결 불가). 지금 응답에서 리뷰끼리 이어 주는 단서: PL-02 `ReviewAuthorView.Id`(`GetPlaceReviews.cs:30`), 이미지 `Key`(`ReviewImageView`, erd.md 스토리지 키에 회원 ID)
4. **R-RV-03: 리뷰를 지우면 같은 날 다시 써도 된다.** 현재 동작 유지(`Delete_ThenCreateSameDay_Returns201`). 할 일: ADR-009 · usecases.md RV-03의 "미결" 표시를 확정으로 바꾸기(사용자)

## 미결 결정
- ~~응답 `version` + `If-Match`~~ → 바디 필드 `Version`, ADR-011(D6-2)
- **PF-02 착수 전 결정 묶음** (범위 확정 2026-10-27 PM. 정책: 리뷰·이미지 보존)
  - ~~⓪ 보존 방식~~ → **(가) users 행 DELETE + `reviews.user_id` `ON DELETE SET NULL`**(2026-10-27 사용자 결정, Claude 추천 채택)
    - 근거: 연결 불가(PM)를 DB가 보장(고리 자체가 없음) · 특수 값 없음 · 영향 범위(nullable `Review.UserId`)를 컴파일러가 드러냄
    - 기각: (나) users 행 남김 — 연결 불가를 응답에서만 지킴, 이메일·닉네임 UNIQUE/CHECK 수정, 남은 토큰 요청이 성공함, 모든 회원 쿼리에 제외 조건 / (다) 센티널 회원 — 두 번째 탈퇴 회원이 같은 장소·날짜 리뷰를 가지면 R-RV-03 UNIQUE 위반으로 탈퇴 불가(500) → 부분 인덱스 필요, 센티널 제외 규칙
    - 대가: 복구 불가(탈퇴 철회·재가입 시 리뷰 복구 없음 → PM 확인 권장). PG 15+ `NULLS NOT DISTINCT`를 R-RV-03 UNIQUE에 쓰면 안 됨
    - PF-02 설계 때 할 것(사용자): `Review.UserId` nullable 표현, 수정·삭제 권한 검사(`EditReview.cs:47`)가 NULL 작성자에서 거절되는지, PL-02 응답 `author`(Id 없음 + "탈퇴한 사용자"), erd.md·마이그레이션 FK 변경, 이미지 `Key`의 회원 ID
    - 아직 남은 질문: 좋아요·북마크·그룹 멤버십은 지우는가(탈퇴 회원이 누른 좋아요를 지우면 남의 리뷰 좋아요 수가 줄어듦 → PM), 같은 이메일 재가입 = 새 회원
  - ① 탈퇴 트랜잭션도 처음에 users 행을 잠그는가 — 안 잠그면 탈퇴 중 같은 회원의 합류가 끼어들어 users DELETE가 RESTRICT 오류(500). 정합성은 RESTRICT가 지키므로 실패 표시 문제. 잠근다면 잠금 순서 users → groups(ADR-010 후속)와 같게. 근거: R-GR-02에 닿는 경로는 같은 users 잠금을 거쳐야 줄이 선다(D6-1 변이: 합류만 빼도 5개). ⓪에서 users 행을 남기면 질문 자체가 바뀜
  - ② 탈퇴 후 남은 60분 액세스 토큰의 좋아요·북마크 요청 → `fk_likes_users` 등 FK 위반 500 (원래 "확정된 결정 (D5)" 하위 미결). ⓪에서 행을 남기면 FK 위반 대신 "탈퇴 회원 요청을 어디서 거절하나"로 바뀜
  - ~~③ 오래된 prefix의 탈퇴 회원 스토리지 이미지 정리~~ → 이미지 보존(PM)으로 정리 대상 아님. 남는 질문: 스토리지 키에 들어간 회원 ID가 탈퇴 뒤에도 남아도 되는가(erd.md 스토리지 키 "회원 ID" 행)
- ~~일정 변경안 보류~~ → 채택(2026-10-20, "확정된 결정 (D5)")
- ~~매직 링크 TOCTOU 보완 조합~~ → "확정된 결정 (D5)" ② A3 + Q2 + A1
- ~~프론트 호스팅 위치~~ → 같은 EC2(2026-11-05 사용자, dep-plan.md §0)
- ~~**공개 배포 전 필수: `web/` Next.js 14.2.35 → 16 메이저 업그레이드**~~ → 2026-11-05 16.4.5 완료(운영 취약점 0, 커밋 전). 당시 기록: — `npm audit` critical(RCE 포함 25건, 2026-10-26). 수정판이 16.x뿐(React 19·비동기 요청 API 등 호환성 깨짐). Supabase → API 교체 diff와 섞지 않게 별도 브랜치·커밋 — DEP 전, FE 끝난 뒤
- 운영 메일 발송기 선택 — SES 기울어짐(도메인 구매 A안으로 DKIM 가능, 2026-11-05). (예: AWS SES — 샌드박스는 검증된 수신자만 발송 가능, 데모 계정 운영과 연결) — DEP 5일째
- ~~운영 DB~~ → EC2 안 postgres 컨테이너(2026-11-05 사용자). 백업·디스크 책임 본인 → dep-plan.md 9단계 복원 리허설
- 복원용 `From`(`UserId`·`Nickname`·`Email` 등) 호출 제한 — `internal` + `InternalsVisibleTo`(컴파일 시점, Domain이 Infrastructure 어셈블리 이름을 앎) vs ArchUnitNET(메서드 호출 규칙, 계층 규칙과 묶을 때 가치) vs 이름·주석만. 막을 Application 코드가 생기는 D5에 결정(ADR-008 "결과")
- 중복 저장/미저장 해제 요청을 오류로 볼지 성공(멱등)으로 볼지 — D5
- ~~마지막 멤버의 탈퇴 두 번째 요청~~ → 성공(멱등), "확정된 결정 (D6)"
- 북마크 API: 토글 vs PUT/DELETE — D5. R-BM-02 A안(커밋 순서, 클릭 순서는 클라이언트 책임) → 멱등한 PUT/DELETE 쪽으로 기움. likes(LK-01)도 같은 구조
- ~~배포 대상(단일 VM vs Azure vs Fly.io)~~ → AWS EC2 단일 인스턴스(2026-10-20 사용자 결정)
- 브랜치 접두사 `chore/*` 허용 시 CLAUDE.md 반영
- PR 머지 방식(merge / squash / rebase)
- 이메일 소문자 변환 일치: PostgreSQL `lower()`(DB `LC_CTYPE` 따름) vs C# `ToLowerInvariant()` — 비ASCII에서 다를 수 있음. DB를 띄운 뒤 `SELECT lower('ÄÖ')`로 확인
- D5 테스트 항목(D4에서 이월, Application 계층 필요): 멤버가 남은 그룹은 Application 경로로 삭제되지 않는다(`group_members.group_id` CASCADE 유지의 대가)
- `ReviewContent.Create`의 `string.Normalize`는 짝 없는 서로게이트에서 `ArgumentException`(문서상 동작, 미실측) → 회원 입력이 Result가 아니라 예외가 됨. API 역직렬화(System.Text.Json)에서 이미 걸러지는지 확인 후 결정 — D5
- 정원이 찬 그룹에서 멤버가 나가면 노출된 초대 코드가 다시 유효해짐 — 코드 재발급·만료 필요 여부, GR 유스케이스 상세화 때
- ~~합류의 잠금 순서~~ → users → groups, "확정된 결정 (D6)"
- 고아 이미지 정리 배치 구현 시: 삭제 흐름을 한 곳에 모으기(C안 전환 전제) — D5. 전체 스캔 소요 시간 측정(C안 도입 기준선) — D8
- 강타입 ID `From(Guid.Empty)`는 예외 → 클라이언트가 빈 Guid를 경로로 보내면 500. Api/Application에서 미리 거를지(A) vs `From`이 허용하고 NotFound로 둘지(B) — D5
- ID Guid v7 vs v4: 삽입 성능·인덱스 크기 측정(주석의 "페이지 분할이 적다"는 미측정) — D8. v7은 생성 시각이 ID에 드러남
- ADR 번호: 003(마이그레이션·배포, DEP 3일째)은 `개발_프로세스.md`에 예약됨 → 새 ADR은 008부터. 작성됨: 002 원본과 다른 점(Claude 작성, 2026-10-17 사용자 검토 후 채택), 004 그룹 생애, 005 FK 유지, 006 강타입 ID, 007 엔티티 시각(Claude 작성, 2026-10-13 사용자 검토 후 채택)
- ADR-002 범위 = 스키마 차이만(D3-2 사용자 결정, 근거 erd.md §4). 닉네임 UNIQUE는 포함. 도메인 규칙 차이(리뷰 길이 trim 후 계산, 하루 기준 UTC(원본 "00시"), 닉네임 화이트리스트·NFC)를 어디에 남길지 미정 — `domain.md`에 이미 있으면 정리 불필요
- design-review 🟡(D2-5, 고칠지 사용자 결정): `Group.MaxGroupMembers` vs `Review.MaxImages` 이름 규칙 불일치(`MaxMembers`?) · `GroupErrors`·`PlaceErrors`·`UserErrors` 헤더 주석 없음(`ReviewErrors`만 있음) · ~~`Members`/`Images` 공개 타입 불일치~~ → 의도된 차이로 종료(D3-2)

## 확정된 결정 (D6)
- 그룹 동시 합류(R-GR-03): **(가) 그룹 행 `FOR UPDATE`**, 잠그는 문장과 멤버를 읽는 문장을 나눈다(ADR-010, incident 003)
- 그룹 생성(GR-02): 그룹 + 첫 멤버 `SaveChanges` 한 번. 초대 코드 충돌은 새 코드로 최대 3회, 다 실패하면 `InvalidOperationException`(생성기 고장). 응답 `GroupView(id, name, inviteCode)`, API는 201 + Location
- 회원당 3그룹(R-GR-02·04): users 행 **`FOR NO KEY UPDATE`** 뒤 참여 수 확인 — 합류·생성 모두. **잠금 순서는 항상 users → groups.** 생성은 users만, 탈퇴는 groups만 잠근다
- 합류 오류 우선순위: AlreadyMember·GroupFull이 TooManyGroups보다 먼저(참여 수는 `AddMember` 뒤에 센다)
- 탈퇴(GR-04, R-GR-09): 그룹 행 잠금 뒤 `RemoveMember` → `GroupEmptied`면 같은 트랜잭션에서 삭제. 멤버가 아니거나 그룹이 이미 없어도 성공(멱등)

## 확정된 결정 (D5)
- PL-01 장소 확정(2026-10-21 사용자 결정)
  - **C′**: Find → 없으면 `INSERT ... ON CONFLICT (kakao_place_id) DO NOTHING` → 0행이면 Find. 흐름은 핸들러에(저장소에 숨기지 않음). B(`DO UPDATE`) 기각 — 입력 불신(덮어쓰기 금지) + 다시 고를 때마다 dead tuple. 근거: "다시 고르는 경우가 많다"(추정, 미측정) → 많이 일어나는 경로 1왕복
  - 0행 뒤 Find가 반드시 1행인 근거: ① ON CONFLICT는 미커밋 상대를 기다려 커밋된 경우에만 0행 ② Read Committed 문장마다 새 스냅샷 ③ 장소 삭제 없음. 깨지면 예외(500)
  - 검증 순서: 빠짐·카카오 ID 형식은 항상, 좌표 범위·이름·주소는 **새 장소일 때만**(usecases 1a 반영). 좌표 빠짐은 Api `[Required] double?`
  - `TryAddAsync`는 id만 받고 넘긴 place를 반환(B안) — `FromSql`이 `ComplexProperty`의 `HasColumnName`을 무시(탐침 P1~P3). places에 DEFAULT·트리거 열이 생기면 다시 읽는다
  - 원본 필드(category·roadAddress·phone·placeUrl) 추가는 FE 주간 — DEP(운영 데이터) 전이라 기존 행 NULL 문제 없음. 리뷰 요약(usecases 4단계)은 PL-02로
- BM-01·02 북마크(2026-10-21 사용자 결정)
  - 토글 대신 `PUT`/`DELETE /places/{placeId}/bookmark`(멱등, R-BM-02와 일치). FE 주간에 `PlaceSheet.tsx`가 상태 보고 PUT/DELETE
  - "이미 그 상태"는 200 + `changed: false`(409 아님)
  - 장소 없음(2b)은 **먼저 SELECT** → 404 `Bookmark.PlaceNotFound`(FK 23503을 잡지 않음). FK는 최종 보장으로 남김
  - 별 상태는 **서버가 계산**(A안) — `Nook.Domain/Stars/StarRule.Evaluate`(사용자 작성), 응답에 `star` 포함. 근거: 원본은 같은 규칙이 4곳(PlaceSheet·store.ts 지도·그룹 지도·프로필 개수)에 흩어짐 → 급한 수정·상태 추가 시 서버 1곳·배포 1회. JSON은 문자열(`"gray"`, 숫자 직렬화 철회), 확장 가능 값 — 클라이언트는 모르는 값을 empty로
  - 장소 블라인드가 필요해지면 삭제가 아니라 soft 상태(`hidden_at`) — "삭제 없음" 전제 유지, 읽기·쓰기 정책만 추가
- LK-01 좋아요(2026-10-22 사용자 결정)
  - BM과 다른 점: 리뷰는 삭제 유스케이스가 있다(RV-03, 탈퇴 CASCADE) → 사전 확인과 INSERT 사이의 틈이 실제 경합. `ON CONFLICT`는 FK 위반(23503)을 받지 않아 그대로 두면 500
  - **A안**: 사전 확인(reviews Q1) + `fk_likes_reviews` 23503만 번역 → 404 `Like.ReviewNotFound`(틈 사이 삭제도 같은 404). B안(사전 확인 없이 FK 위반만 잡기)에서 바꾼 이유: D9 카운터로 트랜잭션에 묶이면 오류 뒤 25P02로 계속 못 함(SAVEPOINT 필요), 정상 404를 에러 로그에 섞지 않음. D(`FOR KEY SHARE` 잠금)·C(`INSERT ... WHERE EXISTS`, 0행 의미가 둘이고 23503도 여전히 가능 — 미측정) 기각
  - 저장소 반환은 `AddLikeOutcome { Added, AlreadyLiked, ReviewGone }` — bool로는 둘을 못 가름. Npgsql 예외는 Infrastructure에서 번역
  - 취소(DELETE)도 없는 리뷰는 404 — DELETE는 FK 검사가 없어 0행만으로는 "없는 리뷰"와 "이미 취소"를 못 가름
  - 취소 중 틈 사이 삭제(CASCADE가 like 행을 먼저 지움 → 0행)는 **200 `changed:false` 허용, 번역 안 함** — `liked:false`는 사실(Add의 ReviewGone은 거짓 `liked:true`라 번역 필요했음)
  - API `PUT`/`DELETE /reviews/{reviewId}/like`, 응답 `{ liked, changed }`(좋아요 수는 D9)
  - BM은 사전 확인만 유지("장소는 삭제하지 않는다"가 살아 있는 한 경합 없음). 두 방식이 섞인 이유는 그 불변식 ADR의 결과 절에
  - 미결: `fk_likes_users`·BM의 회원 FK 위반(탈퇴 후 남은 60분 토큰) → 지금은 500. PF-02에서 정함
- **일정 변경(포트폴리오 우선) 채택** (2026-10-20 사용자 결정): D5 → D6 → FE → DEP(1주, AWS EC2 + GitHub Actions) → README 최소판 → D7~D11 → D13 → D14. 원래 할 일은 빠짐없이 배포 후 진행하고, 각 Day 게이트에 "🚀 운영 반영"을 붙인다. 근거 `docs/schedule-option-portfolio-first.md`
- 매직 링크 TOCTOU(Q2 COUNT ↔ INSERT 사이 동시 요청): Q2 경합은 허용하고 **② A3 + Q2 + A1**으로 막는다(2026-10-20 사용자 결정). 막을 대상 = 피해자 받은편지함 + 발송 도메인 평판
  - A3 IP 기준 입구 제한(`RateLimiter`, 본문 불필요) — 표준 방어, 단일 IP 폭주를 막음
  - Q2 이메일 기준 한도(DB) — 순차 요청을 막음
  - A1 워커에서 수신자별 발송 상한 — 분산(여러 IP) 동시 공격으로 Q2를 통과한 몫을 발송 지점에서 막음. 상한값은 `MagicLinkPolicy`에서 가져온다, 버린 건수는 로그(이메일 원문 금지)
  - 전제: **단일 인스턴스·단일 소비자**(`SingleReader`). 소비자가 여러 개가 되면 A1의 무경합 성질이 사라짐 → 공유 저장소(Redis) 또는 B(advisory lock)로 이전 — D11 운영 반영에서 재검토
  - 남는 것: Q2를 통과했지만 메일이 안 나간 토큰 행(원문은 어디에도 없음) — 만료 후 정리 배치가 지움
  - 구현: DEP 5일째(A3·A1), 측정: 같은 이메일 동시 100요청 → 실제 발송 수 ≤ 상한
- AU-02 소모(2026-10-20 사용자 결정)
  - 트랜잭션 경계는 핸들러가 쥔다((가) — `IUnitOfWork.BeginAsync` → `ITransaction.CommitAsync`, 커밋 없이 Dispose = 롤백). 근거: "회원이 없으면 닉네임 필요"는 업무 규칙이라 Application. D6 그룹 합류도 같은 방식
  - 응답: 성공 200 `{accessToken}` · 닉네임 필요 200 `{needsNickname: true}`(오류가 아니라 가입의 다음 단계) · 무효 링크 400 `Auth.InvalidLink`(없음·만료·사용됨 구분 안 함, 401은 프론트의 토큰 만료 공통 처리와 겹치고 ErrorType에 Unauthorized 없음) · 닉네임 형식 400 · 닉네임 중복 409
  - 닉네임은 "회원 없음 + 닉네임 있음"일 때만 `Nickname.Create`로 검사(Q1 전에는 가입자인지 알 수 없다 — 이메일은 Q1 RETURNING에서 처음 나옴). 기존 회원의 nickname은 쓰지 않는다. 사용자 입력에 `From` 금지(빈 값만 검사)
  - 같은 이메일의 서로 다른 링크 동시 소모 → 둘 다 "회원 없음" → 둘째는 실패 → **(a) 500 그대로 수용**(같은 사람, 한쪽은 로그인됨, 데이터 손상 없음). 예상(미측정): UNIQUE 위반이 아니라 Q3가 상대가 잠근 토큰 행을 기다려 교착(40P01) — **D6 재현 후보**
- 매직 링크 요청 순서: 형식 검사 → Q2 → 토큰 생성 → INSERT → Enqueue. INSERT를 먼저 하는 이유 = 저장 전에 메일이 도착하면 링크를 눌러도 인증 실패(사용자 답)
- 인증: 매직 링크(원본과 같은 패스워드리스) + **API 자체 발급** Access 토큰(HS256), **Refresh 없음**, 수명 60분. 로그아웃(AU-03)은 클라이언트가 토큰을 버린다
- 탈취된 토큰은 만료(최대 1시간)까지 유효 — 수용. 필요해지면 refresh 또는 `jti` 차단 목록. 탈퇴 회원 토큰은 "DB에 없는 회원이면 거절"(GR-01 3a)로 막힌다
- 지킬 것: `sub` = 사용자 ID(이메일·닉네임 아님), 토큰 발급은 "본인 확인 완료" 뒤 `AccessTokenIssuer` 한 곳. 개인정보는 클레임에 넣지 않는다
- 인가 기본 잠금: Fallback 정책 `RequireAuthenticatedUser`, 공개 엔드포인트만 `[AllowAnonymous]`
- 가입 여부 노출 방지: 메일 요청은 항상 같은 응답(202), 속도 제한에 걸려도 같은 응답. AU-01의 "이미 가입된 이메일" 안내 삭제
- 매직 링크: 별도 테이블 `magic_link_tokens`(erd.md). 해시 저장, 15분 유효, 이메일별 15분 3회, 만료 후 1일 보관, POST로 소모, 소모와 회원 생성은 한 트랜잭션
- 로그인 수단 테이블(`provider, provider_subject`) 분리는 소셜 로그인 도입 때(지금은 이메일 하나)
- 메일 백그라운드 발송: B안 `Channel` + `BackgroundService`(같은 프로세스). 재시작 시 유실 수용(15분 일회용, 회원 재요청). outbox(C안)는 필요해질 때. 운영 발송기 미등록 시 앱 시작 실패(콘솔 발송기는 Development만) — DEP 5일째에 실제 발송기
- 매직 링크 요청: 이메일 형식 오류만 400(DB를 보지 않아 가입 여부 비노출), 그 밖에는 속도 제한 포함 항상 202
- `Result`·`Error`·`ErrorType`은 `Nook.SharedKernel`(Domain·Application·Api·테스트가 명시 참조). 전이 참조 차단(엔티티 차단)은 유지. Application 공개 메서드는 Domain 엔티티를 노출하지 않는다
- 링크는 설정값 `MagicLink:LinkBaseUrl` + `#token=`(프래그먼트 — 서버·프록시 로그에 안 남음)
- sub 읽기는 한 곳(`User.GetUserId()` 류)에 모은다. sub 없음·비Guid는 401이 아니라 **예외 → 500 + 에러 로그**(서명된 클레임의 형식 위반 = 서버 버그, D2 Result/예외 기준). 로그에 토큰 원문 금지(사유·경로만). D5-2 사용자 결정

## 확정된 결정 (D4)
- 테스트 코드는 대부분 Claude 작성, 사용자는 결과 예측·빨간불 확인(시간 사정, D4-1 사용자 요청)
- 경계값 테스트는 상수가 아닌 숫자 리터럴로(SSoT를 지키는 테스트는 SSoT를 참조하지 않는다)
- DB CHECK는 raw SQL UPDATE로 앱을 우회해 검증(23514 + `ConstraintName`까지)
- 리뷰 본문: NFC 정규화 → 앞뒤 공백·서식 문자(Cf: U+200B·U+FEFF 등) 제거 → 코드 포인트로 길이 검사. 안쪽 서식 문자는 유지(ZWJ 이모지). DB CHECK에 `IS NFC NORMALIZED` 추가는 하지 않음(마이그레이션 회피, 도메인이 정규화한 값만 저장)
- `AddMember`: `joinedAt < CreatedAt`이면 `ArgumentOutOfRangeException`(호출 코드 실수), 같은 시각 허용. 시각 검사는 Result 실패(중복·정원)보다 먼저

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
