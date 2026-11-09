# DEP 배포 계획 (2026-11-05 확정)

> Claude 작성(사용자 요청). 결정은 사용자. `docs/개발_프로세스.md` §DEP 표(1~7일)를 **보안 1순위·비용 2순위** 기준으로 다시 짠 실행 순서다.
> 진행 상태는 `docs/progress.md` "▶ 다음"이 기준이고, 이 문서는 단계 정의와 근거를 담는다.

## 0. 확정된 결정 (2026-11-05, 사용자)

| # | 항목 | 결정 | 근거 |
|---|---|---|---|
| 1 | 프론트 위치 | **같은 EC2**(Caddy가 web·api 앞단) | BFF 쿠키가 같은 출처 → CORS 불필요, 추가 비용 0 |
| 2 | 운영 DB | **EC2 안 postgres 컨테이너**(compose) | 비용 최소. 대가: 백업·디스크 책임 본인 → 9단계 복원 리허설 필수 |
| 3 | 사용자 업로드 이미지 | **S3로 교체**(로컬 디스크 `App_Data/images` 구현체 → S3 구현체) | 컨테이너 교체 시 유실 방지, 암호화·백업 |
| 3a | S3 구현 세부 (2026-11-09) | **코드는 5단계 전에 구현**(도메인 대기 중, `567a885`) · 인터페이스 `IImageStorage`(리뷰·프로필 공용, `50431ca`) · **API 프록시 유지**(버킷 완전 비공개, `GET /images/{key}`가 S3에서 읽음, FE 변경 없음) · 테스트는 **`adobe/s3mock`** Testcontainers | presigned URL은 만료로 immutable 캐시가 깨지고 FE·BFF 수정 필요, CloudFront+OAC는 리소스·도메인 결정과 엮임 → 기각. MinIO는 공개 이미지가 없어짐(Docker Hub 없음·quay 401 실측) |
| 4a | 컨테이너 레지스트리 | **GHCR → ECR** (원래 계획 변경) | private GHCR pull엔 서버에 GitHub PAT(장기 비밀값) 필요. ECR은 인스턴스 역할로 pull |
| 4b | 배포 경로 | **SSH → SSM Run Command** (원래 계획 변경) | 22번 포트를 닫고 SSH 키를 GitHub에 두지 않음. 접속도 Session Manager |
| 5 | 도메인 | **A. 저렴한 도메인 구매**(5단계 전까지). 등록처 **Route 53**(2026-11-09). 이름 **`fillyournookwithstars.com` 구매(2026-11-09, PM 결정)** — $16/년(갱신 동일), 1년·자동 갱신·개인정보 보호, 등록자 이메일 인증 완료. 이전 후보 `nookmap.app`은 채택 안 함. **`.com`은 HSTS 프리로드 TLD가 아님** → HTTP→HTTPS 리다이렉트(Caddy 기본 동작 — 5단계에서 실측)와 `Strict-Transport-Security` 헤더를 직접 보내야 함(5단계 확인 항목). **SES(2026-11-09)**: 서울 리전 도메인 자격 증명, Easy DKIM RSA 2048, 사용자 지정 MAIL FROM 끔(DMARC는 DKIM 정렬만으로 통과 — 필요하면 나중에 켬, 재생성 불필요), DMARC `p=none`(수신함이 없어 `rua` 없음) → 테스트 발송 `dkim=pass`·`dmarc=pass`(Gmail 받은편지함) 확인 후 **`p=quarantine`으로 올림 ✅ 2026-11-09**(권한 서버·8.8.8.8 조회 확인). 몇 주 문제없으면 `p=reject` 검토. 샌드박스에서는 미인증 주소로 발송 거절 + 워커 발송이라 화면엔 성공 → 운영 승인 후 외부 주소 실발송 확인 필수 | HTTPS(쿠키 `Secure`), SES DKIM/DMARC(매직 링크 도착률), 카카오 도메인 등록. DuckDNS(B)·IP+HTTP(C) 기각. Route 53: 계정·비밀값이 늘지 않고 SES·ACM 레코드 연동(Cloudflare 연 ~$10 저렴하나 계정 추가) |
| 6 | 공개 시점 (2026-11-07) | **(a) 5단계 SG 80·443은 내 IP만 → 7단계 완료(보안 + 개인정보 처리방침·수집 동의) 뒤 0.0.0.0/0으로 전환** | 5단계(문 열기)가 7단계(IP RateLimiter·메일 상한·ForwardedHeaders)보다 앞서 방어 없는 공개 구간이 생김. 순서는 유지하고 문만 늦게 연다. (b) 7단계 선행은 기각 |
| 7 | 계정 플랜·리전 (2026-11-07) | **A. Paid plan 업그레이드 → 서울(`ap-northeast-2`)**. 순서: 루트 MFA → Budgets 알림 → 업그레이드(크레딧 유지·되돌림 여부 화면 확인) → 서울 활성화 | Free plan은 시드니(`ap-southeast-2`) 고정, 다른 리전은 업그레이드 + 고급 기능 활성화 필요(실측 팝업). 시드니(B)는 지연·**개인정보 국외 이전**(§5 처리방침에 고지 필요)·약 6개월 기한·계획 수정 → 기각 |

"이미지" 용어 주의: 4a는 **컨테이너 이미지**(앱 실행 묶음), 3은 **사용자 업로드 사진**. 서로 무관하다.

## 1. 단계 (진행 순서)

| # | 단계 | 핵심 | 작성 | 상태 |
|---|---|---|---|---|
| 1 | Next 16 보안 업그레이드 | critical RCE 해소, 별도 브랜치 `fix/next16-security` | Claude(사용자 요청) | ✅ `37de343` · 브라우저 확인은 2단계 compose에서 통과 |
| 2 | 컨테이너화 | api·web Dockerfile(multi-stage, non-root), compose(api·web·postgres·caddy), 마이그레이션 1회성 컨테이너, 로컬 3흐름 | Claude 초안 → **ADR-003 사용자** | ✅ 초안·로컬 compose 기동·curl 스모크(Origin·401/403·API 비노출) · 브라우저 compose 전용 확인(2026-11-07, 3300 리스너 Docker만 실측): 가입·닉네임 POST, 지도, 장소·리뷰 작성, 프로필 사진 → 볼륨에 UID 1654 소유로 저장·표시. 앱 로직 A·B는 같은 날 dev 서버에서 통과. 주의: dev(`npm run dev`)와 compose가 같은 3300에 IPv6/IPv4로 함께 뜰 수 있음 — 확인 전 `netstat`으로 리스너 확인 / ✅ ADR-003(Claude 작성, 사용자 요청) |
| 3 | CI (PR 게이트) | build·test(Testcontainers), `next build`, `npm audit --omit=dev`·`dotnet list package --vulnerable`, Trivy, `main` 보호. lint 실패 조건 여부 결정 | Claude | 🟡 `feat/dep-ci`(2026-11-07) — 결정: lint 오류 = 실패(5건 수정), `main` 보호는 규칙 없이 CI 표시 + 로컬 훅(private·Free 제약), `web/.github`·`.githooks` → 루트로. Trivy 대응으로 chiseled·npm 제거. 로컬 재현: Release `-warnaserror`·NuGet/npm audit 0·`next build`·Trivy 3이미지 exit 0. ✅ 첫 Actions 실행 4 job(api·web·audit·images) 통과(2026-11-07, 사용자 확인) / ✅ 화면 확인 3건 · PR #12 병합 |
| 4 | AWS 계정 보안 기본 | 루트 MFA·루트 키 없음, IAM Identity Center, Budgets 알림, CloudTrail, 서울 리전 | 사용자(콘솔) + 가이드 | |
| 5 | 인프라 | EC2 t4g.small(ARM), IMDSv2 강제, EBS 기본 암호화, SG 80·443만(22 닫음) — **처음엔 내 IP만(결정 6)**, SSM 에이전트·인스턴스 역할, 탄력적 IP, 도메인 DNS, S3(이미지·백업, 퍼블릭 차단, SSE) | 사용자 + 가이드 | |
| 6 | 비밀값 | SSM Parameter Store SecureString, 배포 시 인스턴스 역할로 읽기. 리포·이미지·로그 금지 | 협업 | |
| 7 | 공개 전 앱 보안 | RateLimiter(IP·이메일), 메일 상한(A1), SES 발송기, `IncludeErrorDetails=false`, **ForwardedHeaders(신뢰 프록시 명시)**, **BFF Origin 검사 허용 Origin 설정값화**, HSTS·보안 헤더, `/health`, **개인정보 처리방침·수집 동의(아래 §5)**. **끝나면 SG 80·443을 전체 공개로 전환(결정 6)** | 협업 | 🟡 2026-11-09 코드 완료분: ForwardedHeaders·IP RateLimiter(PR #34) · `/health/live`·`/health/ready`(`398887c`) · `UseHttpsRedirection` 제거·`IncludeErrorDetails=false` 테스트 고정(`8ab8a9d`) · Caddy 보안 헤더 + **강제 CSP**(`119c90a`, Report-Only 측정 → 강제 모드 브라우저 확인, `'unsafe-eval'` 없음). BFF `/api/auth/*` 5곳 Origin 검사·`Retry-After` 전달·429 화면(PR #37) · **A1 수신자별 발송 상한**(워커, 슬라이딩 로그 3/15분, 잠금 없음 = 단일 소비자 전제) — 측정: 같은 이메일 동시 100요청 → Q2 통과 8건 → 발송 3통, A1 제거 변이 시 12통 · **SES 발송기 코드**(`Email:Provider=Ses`, `From`·`Region` 필수, UTF-8 Charset 테스트 고정, 자격 증명 = 인스턴스 역할). **남은 것**: SES 실제 발송 확인(도메인 DKIM·운영 승인 뒤, 인스턴스 역할 `ses:SendEmail`), 개인정보 처리방침·수집 동의(§5 — 백업 보존 약 5주도 기재), CSP nonce(개선) |
| 8 | CD | `main` push → arm64 이미지 → ECR(sha 태그) → GitHub OIDC(신뢰 정책 `main` 한정) → SSM → pull·마이그레이션·`compose up`·헬스체크 → 실패 시 직전 태그 롤백 | Claude | |
| 9 | 운영·복구 | `pg_dump` → S3 주기 백업, **복원 리허설 1회**, EBS 스냅샷(DLM), 로그 로테이션, 사고 대응 런북, **종료 런북 갱신(`aws-shutdown-runbook.md`)** | 협업 | 🟡 2026-11-09 로컬 선행: 결정(사용자) **하루 1회(RPO 24h) · 보존 매일 7 + 주 4**(S3 수명 주기). `deploy/scripts/db-backup.sh`(`-Fc`, 권한 600, `pg_restore --list` 점검)·`db-restore-check.sh`(버리는 `postgres:17`에 `--exit-on-error` 복원 → 테이블별 행 수 대조·시간). 로컬 통과, 잘린 덤프·덤프 뒤 쓰기는 실패로 잡음(실측). **남은 것**: S3 업로드·타이머(systemd), EC2 기준 RTO 전체 측정, 운영 리허설 판정은 원본 대조 대신 `/health/ready` 200 + 핵심 테이블 비어 있지 않음(쓰기 중 대조는 거짓 실패), 사진은 덤프 밖 → S3 버전 관리·수명 주기(5단계 버킷 생성 때), **백업 보존 = 탈퇴 회원 데이터가 최대 약 5주 남음 → 개인정보 처리방침 파기 범위에 기재(§5)** |
| 10 | 게이트 + Pillar 점검 | `개발_프로세스.md` DEP 게이트 4개 + 아래 표를 측정값·화면으로 확인 | 사용자 확인 | |

## 2. AWS 보안 Pillar 점검표 (계획 기준 — 10단계에서 실제로 확인)

| Pillar | 지키는 방법 | 남는 위험·대가 |
|---|---|---|
| Identity & access management | 루트 MFA·키 없음, Identity Center, **장기 액세스 키 0개**(Actions = OIDC, EC2 = 인스턴스 역할), 역할별 최소 권한(ECR push / SSM SendCommand / S3 prefix) | OIDC 신뢰 정책을 `repo:…:ref:refs/heads/main`으로 좁히지 않으면 다른 브랜치·포크가 역할 획득 |
| Detection | CloudTrail, Budgets, 구조화 로그(인증 실패·429, 이메일 원문 금지), CI 의존성·이미지 스캔, GitHub secret scanning | GuardDuty는 30일 무료 후 유료 → 선택. CloudWatch Logs 비용 → 로컬 로그 + 로테이션으로 시작 |
| Infrastructure protection | SG 80·443만, DB 포트 미노출(compose 내부망), IMDSv2, non-root 컨테이너, OS 자동 보안 패치, Next 16, RateLimiter, `/_next/image` 외부 URL 차단(1단계) | 단일 인스턴스라 ALB·WAF 없음(비용상 의도). L7 방어는 앱·Caddy만 |
| Data protection | Caddy 자동 TLS + HSTS, EBS·S3·백업 암호화, SecureString, httpOnly 쿠키(BFF), 매직 링크 해시 저장 | DB가 같은 EC2 → 인스턴스 유실 = DB 유실. 백업·복원 리허설로 보완 |
| Incident response | 불변 sha 태그로 즉시 롤백, 복원 리허설, EBS 스냅샷, 런북(키 유출 시 교체, SG 격리) | 런북을 실행해 본 적 없으면 무의미 → 리허설 1회 필수 |

예상 비용(대략, Budgets로 실측 확인): t4g.small 약 $12/월 + 퍼블릭 IPv4 약 $3.6/월 + EBS 20GB 약 $2/월 + 도메인 연 $16(실제) + 호스팅 영역 $0.50/월. ECR·S3·Parameter Store·SES는 거의 0.
t4g.micro(1GB)는 .NET + Next + Postgres + Caddy 동시 구동 시 OOM 우려 → small에서 시작, 측정 후 조정.

## 3. 1단계 결과 (2026-11-05)

| 항목 | 이전 | 이후 |
|---|---|---|
| next / react | 14.2.35 / 18.3 | **16.4.5(정확 고정)** / 19.3 |
| 운영 의존성 취약점(`npm audit --omit=dev`) | critical 1·high 3·moderate 6 | **0** |
| 전체(dev 포함) | 위와 같음 | moderate 8 — 전부 `eslint-config-next` → typescript-eslint → minimatch(dev lint 체인, 빌드 결과물 미포함) |
| `tsc --noEmit` · `next build` | 통과 | 통과 |
| 클라이언트 JS | 라우트 표: 공통 87.3 kB · `/review/new` 96.3 kB · Middleware 26.6 kB | `.next/static` JS 23개 합계 782.7 kB raw / **246.0 kB gzip**. Turbopack 기본이 되어 라우트 표가 안 나옴 → 직접 비교 불가 |

코드 변경: `cookies()`·`params`·`searchParams` Promise화(`lib/api/server.ts`의 `readSignupPending` async, `user.server.ts`, `api/auth/signup`, `onboarding/page`, `review/edit/[id]/page`, `api/nook/[...path]`) · `middleware.ts` → `proxy.ts`(함수명 `proxy`) · `.eslintrc.json` → `eslint.config.mjs`(flat) · `lint` 스크립트 `eslint .` · `tsconfig.json`(Next가 자동 수정: `jsx: react-jsx`, `target: ES2017`) · postcss override 주석 갱신(유지) · `next.config.js` 이미지 설정.

`next.config.js` 이미지(Claude 작성, 사용자 요청): `remotePatterns: "**"` 삭제 + `localPatterns: /onboarding/**`. `next start` 실측 — 슬라이드 200, 외부 URL 400, 다른 로컬 파일 400, `/map` 쿠키 없음 307 → `/login`.

남은 것:
- 브라우저 3흐름(로그인·지도·리뷰) 확인 — `docs/design/fe-manual-checklist.md`. **2단계 compose 환경에서 한 번에 볼지 지금 볼지 사용자 결정 대기**
- 새 lint 오류 5건(react-hooks v7: `set-state-in-effect` — `auth/magic-link/page.tsx:32`, `MapScreen.tsx:208·215`, `ProfileScreen.tsx:60` / `immutability` — `MapScreen.tsx:87`) + 경고 4건. 동작 변화 아님, 규칙 추가. incident 004 코드와 얽혀 업그레이드 diff에 섞지 않음 → 부채. CI 실패 조건 여부는 3단계

## 4. 미리 알고 있어야 할 함정

- **BFF Origin 검사 × 프록시**: `api/nook/[...path]/route.ts`가 `origin !== request.nextUrl.origin`이면 403. Caddy 뒤에선 Next가 보는 자기 주소(`http://web:3300` 류)와 브라우저 Origin(`https://도메인`)이 달라 **모든 POST가 403**. 검사를 풀면 CSRF 방어가 사라짐 → 허용 Origin을 설정값으로 명시(2·7단계)
- **ForwardedHeaders**: Caddy 뒤에서 모든 요청 IP가 프록시 IP → IP 기준 RateLimiter가 전원을 한 명으로 봄. 반대로 `X-Forwarded-For`를 무조건 믿으면 IP 위조 우회 → 신뢰 프록시(KnownNetworks/KnownProxies) 명시(7단계)
  - ✅ 2026-11-09 `c474a7a` — 실제 경로는 **Caddy → web(BFF) → api 2단**이라 api가 보는 상대는 web. 고리 3개: Caddy가 들어온 XFF를 버리고 실제 IP로 덮어씀(`trusted_proxies` 없음) → BFF `apiFetch`가 XFF 맨 오른쪽만 넘김 → api는 `ForwardedHeaders:KnownNetworks`(backend `10.89.0.0/24` 고정)에서 온 것만 믿음. **신뢰 목록이 비면 미들웨어는 "모두 믿음"**(변이로 실측) → 비면 켜지 않음
  - 로컬 compose 실측: XFF를 매번 위조한 매직 링크 6회 → 6번째 429, api 로그에 실제 상대 IP(`172.22.0.1`)
  - 한도(시작값, 거절 로그로 조정): 전체 IP당 300/분 · 매직 링크 요청·소비 각각 IP당 5/10분(칸 = IP + 경로). 설정 `RateLimit:*`
  - 남은 것: BFF `/api/auth/*`가 `Retry-After` 헤더를 넘기지 않고 화면도 429 문구가 없음(FE). IPv6를 열면 /64 단위로 묶어야 함(지금은 주소 하나 = 한 칸)
- **compose 네트워크 설정 변경 = `down` 후 `up`** (2026-11-09 실측): 서브넷을 바꾸면 compose가 네트워크는 다시 만들지만 기존 컨테이너는 이어 붙이기만 해서 **서비스 이름 DNS 별칭(`db`)이 빠짐** → migrate가 `Resource temporarily unavailable`(DNS SERVFAIL). 볼륨은 `-v` 없이 내리면 유지
- **ARM 이미지**: t4g = arm64. Actions에서 arm64 빌드(arm 러너 또는 buildx) 필요(8단계)
  - 2026-11-09 결정(사용자): **교차 빌드 + 에뮬레이션** — api는 SDK를 `$BUILDPLATFORM`으로(DLL은 아키텍처 무관, 로컬 15s), **migrate 번들과 web은 대상 플랫폼(QEMU)**. 번들 도구가 `--runtime linux-arm64` 빌드 결과를 직접 로드해서 교차 빌드하면 `Could not load file or assembly 'Nook.Api'`(실측). web은 `npm ci`가 arm64 네이티브 바이너리를 받아야 함
  - 로컬 arm64 빌드: api 15s · migrate 854s · web 861s. 에뮬레이션 실행 확인: api RID `linux-arm64`·live 200, web `/login` 200, 번들 `--help` 정상
  - **PR CI는 x86 유지**(사용자 결정 — PR마다 약 29분은 김). arm64는 8단계 배포 작업에서 빌드하고 **그 이미지를 Trivy로 다시 스캔**한다
- **배포 스크립트**: `deploy/scripts/deploy.sh <태그>` + `deploy/compose.prod.yaml`(ECR 이미지·S3·api egress·80/443 `!override`). 로컬 리허설(2026-11-09): good 배포 성공 → bad(api 즉시 종료) 배포 → ready 60s 실패 → good 자동 롤백, exit 1, 기록 태그 good 유지. **롤백은 이미지만** — 마이그레이션은 확장 → 전환 → 축소로 직전 버전과 호환되게
- **S3 × compose 네트워크** (2026-11-09): `backend`가 `internal: true`라 api가 S3에 못 나간다(연결 시간 초과). Provider를 `S3`로 바꿀 때 **api만** egress 네트워크에 추가(5단계). 로컬 compose는 LocalDisk라 지금은 열지 않음(최소 권한)
- **IMDSv2 홉 제한**: 컨테이너 안 SDK가 인스턴스 역할 자격 증명을 받으려면 응답이 Docker 브리지를 한 번 더 건넌다 → `HttpPutResponseHopLimit` **2 이상**. 1이면 "자격 증명 없음"으로만 보인다(5단계 EC2 생성 시)
- **S3 없는 키 = 403**: 역할에 `s3:ListBucket`이 없으면 S3는 없는 키에 404 대신 403을 준다. `S3ImageStorage`는 404만 "없음"으로 보고 403은 설정 사고로 올려 500 → 인스턴스 역할 정책에 버킷 `s3:ListBucket` + 객체 `s3:GetObject`·`PutObject`·`DeleteObject`(5단계)
- **route53domains는 us-east-1 전용**: 리전 제한 SCP를 걸면 이 서비스는 예외로 둬야 한다. CLI는 `--region us-east-1`
- **`nook-readonly` 축소 검증 남음**(2026-11-09 추가): `ViewOnlyAccess + SecurityAudit`로 줄였지만 데이터 든 버킷이 없어 "거부되는지"는 미확인 → 이미지 버킷 생성 후 `s3api get-object --profile nook-readonly`가 `AccessDenied`인지 확인
- **SES·DMARC 확인**(2026-11-09 추가): 운영 승인 후 실제 매직 링크를 외부 주소로 받아 `dkim=pass`·`dmarc=pass`·한글 제목 확인. 샌드박스에서는 워커 발송 실패가 화면에 안 보임(로그만)
- **5단계 S3·IAM 결정**(2026-11-09): 버킷 2개 **계정 리전 네임스페이스**(`nook-prod-{images,backups}-<계정ID>-ap-northeast-2-an` — 다른 계정이 같은 이름을 못 만듦, 무작위 접미사 불필요). 둘 다 버전 관리·SSE-S3·퍼블릭 차단·ACL 끔. 이미지 수명 주기 `expire-noncurrent-35d`(백업 보존 약 5주와 맞춤). 백업은 침해 대비로 버전 관리 켬. 인스턴스 역할 `nook-ec2` 인라인 정책 = **`deploy/iam/nook-ec2-inline-policy.json`**(이미지 `reviews/`·`avatars/` Get·Put·Delete + 버킷 List / 백업 **`daily/`·`weekly/` Put만**(사용자 작성) / SES `no-reply@`만). 백업 키 접두사는 이 정책이 기준 → 9단계 스크립트·수명 주기도 같은 이름. `s3:AbortMultipartUpload`는 주지 않음 — 남은 조각은 백업 버킷 수명 주기 "미완료 업로드 7일 삭제"(9단계)가 치움. 역할은 api 컨테이너·호스트 백업 스크립트가 공유(IMDS) → 백업이 쓰기 전용이라 api 침해 시에도 읽기·삭제 불가
- **5단계 실호출 확인 목록**: SSM 세션에서 인스턴스 역할로 ① 이미지 `reviews/` Put·Get·Delete, 없는 키 Get = 404(403이면 ListBucket 누락) ② 이미지 다른 접두사 Put = 거부 ③ 백업 `daily/` Put 성공, 같은 키 Get·Delete·List = 거부 ④ SES `no-reply@` 발송(샌드박스면 수신 자격 증명 권한 문제 가능 → 결과로 판단), 다른 From = 거부 ⑤ `nook-readonly`로 이미지 객체 Get = 거부
- **SES 리전**: 서울만 인증·승인. 콘솔이 마지막 리전을 기억해 다른 리전에 만들 수 있음 → 시드니는 SCP로 차단(2026-11-09), 그래도 작업 전 리전 확인

## 5. 개인정보 처리방침·수집 동의 (2026-11-07 추가, 7단계 · 공개 조건)

원본 nook도 비어 있던 부분(원본 `NOOK_PRD_리뷰.md` P3, 설정 "서비스 약관 및 방침" = `RowDisabled`). 수집 항목: 이메일·닉네임·성별(필수, R-US-04)·생년월일(선택, R-US-05)·사진(리뷰·프로필). 실제 사용자를 받기 전(SG 전체 공개 전)에 끝낸다. 법률 검토는 Claude가 대신하지 못한다.

| 영역 | 할 일 | 작성 |
|---|---|---|
| 문서 | 처리방침 본문 — 수집 항목·목적(성별 필수 근거 포함)·보관 기간·파기·처리 위탁(AWS·SES)·이용자 권리 | **사용자(PM)**, Claude는 항목 틀 |
| 정책 결정 | 만 14세 미만 가입 처리(생년월일이 선택이라 확인 수단부터), 선택 항목 별도 동의, 탈퇴 후 파기 범위(PF-02와 연결) | **사용자(PM)** |
| DB | 동의 기록 — 예: `users.privacy_consented_at`·`privacy_policy_version`(방침 개정 시 재동의 판단) | 협업 |
| API | 가입 요청에 동의 필수, 없으면 400 | 협업 |
| FE | 가입 동의 체크 + 처리방침 링크, `/privacy` 페이지, 설정 "약관 및 방침" 활성화 | Claude |

**2026-11-09 진행**: 결정(사용자, PM 확인 대기) — 성별·생년월일 둘 다 선택 + 별도 동의(쓰는 기능 없음 → 최소 수집) · 만 14세 미만 가입 불가(자기 신고 필수 + 생년월일 있으면 서버 거절) · 탈퇴 후 리뷰 유지 + 안내 · 접속 로그 30일. 구현: `PrivacyConsent`·R-US-06·마이그레이션 `AddPrivacyConsent`(`ck_users_optional_consent`는 NOT VALID — 옛 개발 행 1개 실측) · 가입 폼 체크 3개 · `/privacy` 초안 · 설정 링크 · **탈퇴 확인 문구 정정**("모든 데이터 삭제" → 리뷰는 남음). 처리방침 초안 `docs/design/privacy-policy.md`(`[PM]` 표시). **남은 것(PM)**: 선택 항목 목적·위탁 고지·카카오 제3자 제공 여부·보호책임자·시행일 → 확정 시 버전 3곳 동시 변경. 재동의 흐름(방침 개정 시) 미구현


## 6. 4단계 진행 기록 (2026-11-07)

> 전체 정리·용어(계정/프로젝트/팀/세션/신원) 구분은 **`aws-account-setup.md`**

신규 가입이 **새 시작 경험**(Builder ID 가입 · 프로젝트 · Free plan · 시드니 고정)으로 진행돼 계획과 순서가 달라졌다. 결정 7 → Paid 업그레이드 → **고급 기능 활성화**(Organizations, 되돌릴 수 없음, **지출 한도 삭제**).

계정 구조(고급 기능이 생성):

| 계정 | 역할 | 비고 |
|---|---|---|
| nook Management Account | 결제·조직·Budgets·CloudTrail 조직 트레일 | 관리 계정. SCP 적용 안 됨. 워크로드 두지 않음 |
| nook Identity Delegated Admin | IAM Identity Center(**`us-east-1`**) 위임 관리 | 팀 이름 `nook` |
| nook-prod (구 Rubber Ducking) | nook 리소스 | 프로젝트 이름만 변경. 콘솔의 **계정 이름**은 Organizations → AWS 계정 → 작업 → 계정 이름 업데이트(반영 최대 4시간) |

로그인 경로: 평소 = Builder ID → 계정별 `AccountFullAccessRole`, 비상 = 관리 계정 루트.

| 항목 | 상태 |
|---|---|
| 루트 MFA | ✅ `root-phone-totp`·`root-pc-passkey`, 루트 액세스 키 없음, 재로그인 MFA 확인. 루트 비밀번호는 가입 때 없었음(Builder ID 비밀번호와 별개) → 재설정으로 생성 |
| Builder ID MFA | ✅ `builderid-phone-totp`·`builderid-pc-passkey`. 복구 이메일 필수(로그인 이메일과 달라야 함) — 현재 같은 Gmail 별칭(독립 경로 아님, 추후 다른 메일 서비스로) |
| Budgets | ✅ `nook-zero-spend`, `nook-monthly-20`(실제 50·85·100%, 예측 100%) |
| CloudTrail | ✅ 조직 트레일 `nook-org-trail`(서울 홈, 다중 리전, 관리 이벤트만, KMS·CloudWatch·재귀 로깅 끔), 로깅 중 |
| 지원 플랜 | ✅ Basic(유료 지원 없음) |
| SCP | `ManagedAccountSecurityControlPolicy`(조직 이탈·관리 역할 보호) 유지 · `BudgetsSpendLimitDeny*` 4개 미연결 · **`AdvancedModeRegionRestrictionSecurityControlPolicy`의 `RegionFloor`에 `ap-northeast-2` 추가**(문서 공식 방법) → CloudShell `aws ec2 describe-instances --region ap-northeast-2` = `[]` 통과 |
| ✅ 콘솔 리전 메뉴 | SCP 수정 후에도 서울이 "콘솔에서 제한" → 원인은 반영 지연이 아니라 **통합 설정 "표시되는 리전"**(SCP와 별도인 계정별 콘솔 표시 목록). nook-prod에 버지니아·오리건·서울·시드니로 저장 → 서울 표시. 상세 `aws-account-setup.md` 4절 17단계·5절 |
| ⏳ 남음 | S3 계정 퍼블릭 액세스 차단(nook-prod), Identity Center 사용자·권한 세트, 로컬 AWS CLI + SSO |

주의: `RegionFloor`는 AWS가 만든 정책이다. 서울이 다시 막히면 이 정책부터 본다. 시드니(`ap-southeast-2`)는 자원 조회 후 제거 검토.
