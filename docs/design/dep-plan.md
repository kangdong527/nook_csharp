# DEP 배포 계획 (2026-11-05 확정)

> Claude 작성(사용자 요청). 결정은 사용자. `docs/개발_프로세스.md` §DEP 표(1~7일)를 **보안 1순위·비용 2순위** 기준으로 다시 짠 실행 순서다.
> 진행 상태는 `docs/progress.md` "▶ 다음"이 기준이고, 이 문서는 단계 정의와 근거를 담는다.

## 0. 확정된 결정 (2026-11-05, 사용자)

| # | 항목 | 결정 | 근거 |
|---|---|---|---|
| 1 | 프론트 위치 | **같은 EC2**(Caddy가 web·api 앞단) | BFF 쿠키가 같은 출처 → CORS 불필요, 추가 비용 0 |
| 2 | 운영 DB | **EC2 안 postgres 컨테이너**(compose) | 비용 최소. 대가: 백업·디스크 책임 본인 → 9단계 복원 리허설 필수 |
| 3 | 사용자 업로드 이미지 | **S3로 교체**(로컬 디스크 `App_Data/images` 구현체 → S3 구현체) | 컨테이너 교체 시 유실 방지, 암호화·백업 |
| 4a | 컨테이너 레지스트리 | **GHCR → ECR** (원래 계획 변경) | private GHCR pull엔 서버에 GitHub PAT(장기 비밀값) 필요. ECR은 인스턴스 역할로 pull |
| 4b | 배포 경로 | **SSH → SSM Run Command** (원래 계획 변경) | 22번 포트를 닫고 SSH 키를 GitHub에 두지 않음. 접속도 Session Manager |
| 5 | 도메인 | **A. 저렴한 도메인 구매**(5단계 전까지) | HTTPS(쿠키 `Secure`), SES DKIM/DMARC(매직 링크 도착률), 카카오 도메인 등록. DuckDNS(B)·IP+HTTP(C) 기각 |
| 6 | 공개 시점 (2026-11-07) | **(a) 5단계 SG 80·443은 내 IP만 → 7단계 완료 뒤 0.0.0.0/0으로 전환** | 5단계(문 열기)가 7단계(IP RateLimiter·메일 상한·ForwardedHeaders)보다 앞서 방어 없는 공개 구간이 생김. 순서는 유지하고 문만 늦게 연다. (b) 7단계 선행은 기각 |

"이미지" 용어 주의: 4a는 **컨테이너 이미지**(앱 실행 묶음), 3은 **사용자 업로드 사진**. 서로 무관하다.

## 1. 단계 (진행 순서)

| # | 단계 | 핵심 | 작성 | 상태 |
|---|---|---|---|---|
| 1 | Next 16 보안 업그레이드 | critical RCE 해소, 별도 브랜치 `fix/next16-security` | Claude(사용자 요청) | ✅ `37de343` · 브라우저 확인은 2단계 compose에서 통과 |
| 2 | 컨테이너화 | api·web Dockerfile(multi-stage, non-root), compose(api·web·postgres·caddy), 마이그레이션 1회성 컨테이너, 로컬 3흐름 | Claude 초안 → **ADR-003 사용자** | ✅ 초안·로컬 compose 기동·curl 스모크(Origin·401/403·API 비노출) · 브라우저 compose 전용 확인(2026-11-07, 3300 리스너 Docker만 실측): 가입·닉네임 POST, 지도, 장소·리뷰 작성, 프로필 사진 → 볼륨에 UID 1654 소유로 저장·표시. 앱 로직 A·B는 같은 날 dev 서버에서 통과. 주의: dev(`npm run dev`)와 compose가 같은 3300에 IPv6/IPv4로 함께 뜰 수 있음 — 확인 전 `netstat`으로 리스너 확인 / ✅ ADR-003(Claude 작성, 사용자 요청) |
| 3 | CI (PR 게이트) | build·test(Testcontainers), `next build`, `npm audit --omit=dev`·`dotnet list package --vulnerable`, Trivy, `main` 보호. lint 실패 조건 여부 결정 | Claude | 🟡 `feat/dep-ci`(2026-11-07) — 결정: lint 오류 = 실패(5건 수정), `main` 보호는 규칙 없이 CI 표시 + 로컬 훅(private·Free 제약), `web/.github`·`.githooks` → 루트로. Trivy 대응으로 chiseled·npm 제거. 로컬 재현: Release `-warnaserror`·NuGet/npm audit 0·`next build`·Trivy 3이미지 exit 0. ⏳ 첫 Actions 실행 |
| 4 | AWS 계정 보안 기본 | 루트 MFA·루트 키 없음, IAM Identity Center, Budgets 알림, CloudTrail, 서울 리전 | 사용자(콘솔) + 가이드 | |
| 5 | 인프라 | EC2 t4g.small(ARM), IMDSv2 강제, EBS 기본 암호화, SG 80·443만(22 닫음) — **처음엔 내 IP만(결정 6)**, SSM 에이전트·인스턴스 역할, 탄력적 IP, 도메인 DNS, S3(이미지·백업, 퍼블릭 차단, SSE) | 사용자 + 가이드 | |
| 6 | 비밀값 | SSM Parameter Store SecureString, 배포 시 인스턴스 역할로 읽기. 리포·이미지·로그 금지 | 협업 | |
| 7 | 공개 전 앱 보안 | RateLimiter(IP·이메일), 메일 상한(A1), SES 발송기, `IncludeErrorDetails=false`, **ForwardedHeaders(신뢰 프록시 명시)**, **BFF Origin 검사 허용 Origin 설정값화**, HSTS·보안 헤더, `/health`. **끝나면 SG 80·443을 전체 공개로 전환(결정 6)** | 협업 | |
| 8 | CD | `main` push → arm64 이미지 → ECR(sha 태그) → GitHub OIDC(신뢰 정책 `main` 한정) → SSM → pull·마이그레이션·`compose up`·헬스체크 → 실패 시 직전 태그 롤백 | Claude | |
| 9 | 운영·복구 | `pg_dump` → S3 주기 백업, **복원 리허설 1회**, EBS 스냅샷(DLM), 로그 로테이션, 사고 대응 런북 | 협업 | |
| 10 | 게이트 + Pillar 점검 | `개발_프로세스.md` DEP 게이트 4개 + 아래 표를 측정값·화면으로 확인 | 사용자 확인 | |

## 2. AWS 보안 Pillar 점검표 (계획 기준 — 10단계에서 실제로 확인)

| Pillar | 지키는 방법 | 남는 위험·대가 |
|---|---|---|
| Identity & access management | 루트 MFA·키 없음, Identity Center, **장기 액세스 키 0개**(Actions = OIDC, EC2 = 인스턴스 역할), 역할별 최소 권한(ECR push / SSM SendCommand / S3 prefix) | OIDC 신뢰 정책을 `repo:…:ref:refs/heads/main`으로 좁히지 않으면 다른 브랜치·포크가 역할 획득 |
| Detection | CloudTrail, Budgets, 구조화 로그(인증 실패·429, 이메일 원문 금지), CI 의존성·이미지 스캔, GitHub secret scanning | GuardDuty는 30일 무료 후 유료 → 선택. CloudWatch Logs 비용 → 로컬 로그 + 로테이션으로 시작 |
| Infrastructure protection | SG 80·443만, DB 포트 미노출(compose 내부망), IMDSv2, non-root 컨테이너, OS 자동 보안 패치, Next 16, RateLimiter, `/_next/image` 외부 URL 차단(1단계) | 단일 인스턴스라 ALB·WAF 없음(비용상 의도). L7 방어는 앱·Caddy만 |
| Data protection | Caddy 자동 TLS + HSTS, EBS·S3·백업 암호화, SecureString, httpOnly 쿠키(BFF), 매직 링크 해시 저장 | DB가 같은 EC2 → 인스턴스 유실 = DB 유실. 백업·복원 리허설로 보완 |
| Incident response | 불변 sha 태그로 즉시 롤백, 복원 리허설, EBS 스냅샷, 런북(키 유출 시 교체, SG 격리) | 런북을 실행해 본 적 없으면 무의미 → 리허설 1회 필수 |

예상 비용(대략, Budgets로 실측 확인): t4g.small 약 $12/월 + 퍼블릭 IPv4 약 $3.6/월 + EBS 20GB 약 $2/월 + 도메인 연 $10 안팎. ECR·S3·Parameter Store·SES는 거의 0.
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
- **ARM 이미지**: t4g = arm64. Actions에서 arm64 빌드(arm 러너 또는 buildx) 필요(8단계)
