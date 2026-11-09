# AWS 계정 구성 정리 (2026-11-07, DEP 4단계)

- 작성: Claude 정리(사용자 요청). 실제 화면 문구·오류 메시지·AWS 공식 문서로 확인한 사실 기준. 확인 못 한 추정은 ⚠️로 표시
- 관련 문서: `dep-plan.md` 결정 7·§6(진행 기록), `aws-shutdown-runbook.md`(비용 중단·탈퇴)
- 계정 ID는 이 문서에 전체를 적지 않는다(뒤 4자리만). 비밀번호·MFA 시크릿·복구 코드는 어디에도 적지 않는다

---

## 1. 용어 정리 — 무엇이 무엇인가

AWS 새 시작 경험은 **같은 대상을 화면마다 다른 이름으로 부른다.** 아래 표가 기준이다.

### 1-1. "담는 그릇" (리소스·사람이 들어가는 곳)

| 용어 | 정체 | 우리 것 | 한 줄 요약 |
|---|---|---|---|
| **조직 (Organization)** | AWS Organizations. 여러 계정을 묶는 최상위 단위 | 1개 (`o-…`) | 계정들의 **집** |
| **계정 (AWS Account)** | 리소스·요금·권한의 **격리 단위**. 12자리 ID | 3개 (아래 2절) | 리소스가 실제로 **사는 곳** |
| **프로젝트 (Project)** | 새 시작 경험의 화면 용어. **= 멤버 계정** (활성화 1단계 문구: "프로젝트를 조직의 멤버 계정으로 관리") | `nook-prod`, `nook Identity Delegated Admin` | 계정의 **새 시작 경험식 이름** |
| **팀 (Team)** | 새 시작 경험의 화면 용어. **= IAM Identity Center 인스턴스** (활성화 2단계 문구: "팀원 ID를 저장하는 IAM Identity Center 인스턴스의 이름") | `nook` (리전 `us-east-1`) | **사람 명부** |
| **리전 (Region)** | 데이터센터 묶음. 리소스는 특정 리전에 생김 | 작업 리전 = **서울 `ap-northeast-2`** | 리소스의 **물리적 위치** |

### 1-2. "들어가는 사람" (신원)

| 신원 | 정체 | 비밀번호 | MFA | 용도 |
|---|---|---|---|---|
| **AWS Builder ID** | AWS 계정 **바깥**의 개인 프로필. 새 시작 경험 가입이 이걸로 됨 | 가입 때 만든 것 | `builderid-phone-totp`, `builderid-pc-passkey` | **평소 로그인** — 여기서 각 계정의 역할로 들어감 |
| **루트 사용자 (Root user)** | 관리 계정의 **최고 권한자**. IAM 정책으로 제한 불가 | 2026-11-07에 **새로 만듦**(가입 때 없었음, Builder ID 비밀번호와 별개) | `root-phone-totp`, `root-pc-passkey` | **비상용만** — MFA 관리, 결제 정보, 계정 해지 |
| **역할 (IAM Role) `AccountFullAccessRole`** | 각 계정 안의 역할. Builder ID가 이 역할을 **빌려 쓴다**(assumed-role) | 없음(임시 자격 증명) | Builder ID MFA를 거침 | 콘솔 우측 위 `AccountFullAccessRole/…`의 정체 |
| ⏳ **Identity Center 사용자** | "팀(nook)"에 만들 일상용 사용자 | (예정) | (예정) | 4-3에서 생성 예정 — 권한을 용도별로 나눔 |

> **같은 이메일(`…+aws@gmail.com`)에 신원이 둘이다** — Builder ID와 루트. 비밀번호도 따로다. "이메일이 같으니 같은 계정"이 아니다.

### 1-3. "들어간 상태" (세션)

| 용어 | 정체 |
|---|---|
| **세션 (Session)** | "어떤 신원으로 어떤 계정에 로그인해 있는 상태". 콘솔은 **여러 세션을 동시에** 유지한다(세션 선택 화면) |
| **세션 선택 화면** | `https://signin.aws.amazon.com/console`. 활성 세션 목록 + "세션 추가"(다른 신원·루트 로그인) + "AWS에서 로그아웃"(전체) |
| **세션 이름** | 우측 위 "현재 세션 → 계정 이름"은 **페더레이션 세션이 붙이는 표시값**. 계정 이름을 바꾼 뒤 4시간+ 지나 재로그인(시크릿 창 포함)해도 옛 이름("Rubber Ducking")이 남았다. **기준값은 Account 페이지의 계정 이름**, 계정 구분은 **계정 ID**로 한다. 기능 영향 없음 |

---

## 2. 우리 계정 구조

```
조직 (Organization, 결제는 관리 계정으로 일괄)
│
├── nook Management Account (…2326)   ← 관리 계정 = 돈 + 조직
│     · 루트 사용자: 비상용 (MFA 2개)
│     · Budgets, CloudTrail 조직 트레일, SCP 편집, 계정 이름 변경
│     · ⚠️ SCP가 적용되지 않는 유일한 계정 → 워크로드(EC2 등)를 두지 않는다
│
├── nook Identity Delegated Admin (…6471)   ← 사람 + 권한
│     · IAM Identity Center 인스턴스 "nook" (리전: us-east-1 버지니아)
│     · 4-3에서 일상 사용자·권한 세트를 여기서 만든다
│
└── nook-prod (구 Rubber Ducking, …1194)   ← 서비스 리소스
      · EC2, S3, ECR, SSM, SES — nook의 모든 리소스
      · 작업 리전: 서울 ap-northeast-2
```

| 계정 | 별명 | 하는 일 | 하지 않는 일 |
|---|---|---|---|
| nook Management Account | **돈·조직** | 결제, 예산, 조직 정책(SCP), 조직 트레일, 계정 이름·추가·해지 | 서비스 리소스 생성 |
| nook Identity Delegated Admin | **사람·권한** | Identity Center 사용자·그룹·권한 세트 | 서비스 리소스 생성 |
| nook-prod | **서비스** | nook 배포에 필요한 모든 리소스 | 조직·결제 설정 |

---

## 3. 무엇을 할 때 어디로 가나 (작업 → 세션·리전)

| 하고 싶은 일 | 신원 | 계정(세션) | 리전 |
|---|---|---|---|
| 평소 리소스 작업(EC2·S3·ECR…) | Builder ID | **nook-prod** | **서울** |
| 예산·청구서 확인 | Builder ID | Management | (글로벌) |
| CloudTrail 트레일·로그 버킷 | Builder ID | Management | 서울(트레일 홈) |
| SCP(조직 정책) 보기·고치기 | Builder ID | Management | (글로벌) |
| 콘솔 리전 메뉴에 리전 추가·숨김 | Builder ID | **해당 계정**(예: nook-prod) → 통합 설정 → 계정 설정 → 표시되는 리전 | (글로벌) |
| 계정 이름 변경 | Builder ID | Management → Organizations → AWS 계정 → 작업 → 계정 이름 업데이트 | (글로벌) |
| 사용자·권한 세트(4-3) | Builder ID | **Identity Delegated Admin** | **버지니아 `us-east-1`** |
| 루트 MFA·루트 비밀번호 | **루트** | Management (세션 추가 → "루트 사용자 이메일을 사용하여 로그인") | (글로벌) |
| Builder ID MFA·복구 이메일 | Builder ID | `https://profile.aws.amazon.com` → 보안 | — |
| CloudShell로 API 확인 | Builder ID | nook-prod | 버지니아(CloudShell 허용 리전) |
| 로컬 CLI 조회·검증 | `aws login --profile nook-prod`(만료 시) | `--profile nook-readonly` | 서울(프로필 기본값) |
| 로컬 CLI 변경 | 같음 | `--profile nook-prod`(관리자) — 사용자만 | 서울 |

**로그인 순서(평소)**: `signin.aws.amazon.com/console` → Builder ID(비밀번호 + MFA) → 세션 선택에서 계정 고르기 → 우측 위 **계정 이름과 리전**을 확인하고 작업.

**로그인 순서(비상, 루트)**: 세션 선택 → **세션 추가** → (IAM 사용자 화면이 나오면) **"루트 사용자 이메일을 사용하여 로그인"** → 이메일 → 루트 비밀번호 → MFA.

---

## 4. 오늘 한 일 (시간순)

| # | 한 일 | 결과·근거 |
|---|---|---|
| 1 | AWS 가입 | 새 시작 경험(Builder ID 가입), **Free plan**, 크레딧 $100·182일, 프로젝트 "Rubber Ducking" |
| 2 | 서울 리전 시도 → 막힘 | 팝업: "아시아 태평양(서울) 리전 사용 불가 … 시드니에 할당 … 플랜을 업그레이드한 다음 고급 기능을 활성화" |
| 3 | 에이전트 툴킷 연결 | **보류** — 루트·장기 키로 연결될 위험, 학습 목적, CLI 읽기 명령으로 대체 가능 |
| 4 | **결정 7**: Paid 업그레이드 + 서울 | 시드니(지연·**개인정보 국외 이전**·기간 제한) 기각. 업그레이드 문구: 종량제, "계정은 언제든지 해지" — Free 복귀 언급 없음 |
| 5 | 유료 지원 플랜 확인 | "Business Support+가 현재 활성화되어 있지 않습니다" → Basic(무료) |
| 6 | **고급 기능 활성화** (3단계) | ① 평가: **"활성화하면 지출 한도가 삭제됩니다"**, 프로젝트 → 멤버 계정 ② 팀 이름 `nook` ③ 관리 계정 이메일 = Builder ID 이메일. 되돌릴 수 없음(문서) |
| 7 | 계정 구조 확인 | 세션 선택 화면에 3계정 등장. 관리 계정 자동 로그인 = `AccountFullAccessRole`(루트 아님) |
| 8 | **루트 비밀번호 생성** | 가입 때 정한 것은 Builder ID 비밀번호 → "비밀번호를 잊으셨나요?"로 루트 비밀번호 새로 설정(PowerShell 보안 난수 → Edge 비밀번호 관리자) |
| 9 | **루트 MFA 2개** | `root-phone-totp`(인증 앱), `root-pc-passkey`(Windows Hello). 두 번째 등록 때 "MFA 장치로 인증된 세션 필요" → MFA로 재로그인 후 성공(단계적 인증). 루트 액세스 키 없음 |
| 10 | **Builder ID MFA 2개** | `builderid-phone-totp`, `builderid-pc-passkey`. **복구 이메일이 MFA 등록의 선행 조건**(로그인 이메일과 달라야 함) → `…+awsrecovery@gmail.com`. PM 메일 사용은 거절(관리 계정 접근 경로가 생김) |
| 11 | **Budgets 2개** | `nook-zero-spend`(첫 1센트 알림), `nook-monthly-20`(실제 50·85·100%, 예측 100%) — 지출 한도 삭제의 대체 |
| 12 | 프로젝트 이름 변경 | Rubber Ducking → **nook-prod**. 프로젝트 이름 = 계정 이름이라 **Organizations·Account 페이지의 계정 이름도 함께 바뀜**(별개 아님). 단 세션 표시 이름은 옛 이름 유지(1절 "세션 이름") |
| 13 | **CloudTrail 조직 트레일** | `nook-org-trail`: 서울 홈, 다중 리전, 조직 전체, 관리 이벤트(읽기·쓰기)만, KMS·CloudWatch Logs·Insights·데이터 이벤트 끔, 재귀 로깅 해제, 로그 파일 검증 켬 → 로깅 중 |
| 14 | SCP 점검 | 아래 5절. 서울이 **API에서 막힘** 확인(`ec2:DescribeInstances` … `explicit deny in a service control policy … p-3b9bzps5`) |
| 15 | Identity Center 리전 확인 | **버지니아 `us-east-1`**(오리건·시드니엔 인스턴스 없음) — SCP를 고칠 때 빼면 로그인이 깨지는 리전 |
| 16 | **SCP에 서울 추가** | `RegionFloor`에 `ap-northeast-2` 1줄 추가(문서 공식 방법). CloudShell `aws ec2 describe-instances --region ap-northeast-2` → `[]` = **API 통과** |
| 17 | ✅ 콘솔 리전 메뉴 | 재로그인·4시간+ 뒤에도 버지니아·시드니·오리건만("제한된 리전: 14"). 계정 → AWS 리전(옵트인)은 서울 "기본적으로 활성화됨" → 원인 아님. **원인: 통합 설정 → 계정 설정 → "표시되는 리전"**(계정별 콘솔 표시 목록, SCP와 별도 저장 — 새 시작 경험이 SCP와 같은 3개로 만들어 둔 것으로 추정). 단서는 리전 메뉴 "제한된 리전 → 자세히 보기". nook-prod에서 **버지니아·오리건·서울·시드니** 4개로 저장 → 서울 표시 확인 (2026-11-07) |
| 18 | 종료·탈퇴 런북 | `aws-shutdown-runbook.md` |
| 19 | **S3 계정 퍼블릭 액세스 차단** | nook-prod → S3 → 왼쪽 메뉴 **"계정 및 조직 설정"**(예전 이름 "이 계정에 대한 퍼블릭 액세스 차단 설정") → 편집 → 모든 퍼블릭 액세스 차단. 버킷 0개 상태에서 4개 모두 활성. 같은 화면의 "Storage Lens … 관리자 권한 없음"은 멤버 계정이라 정상 |
| 20 | **CLI: `aws login`** | AWS CLI 2.39.5(`winget install -e --id Amazon.AWSCLI`). `aws login --profile nook-prod`(리전 `ap-northeast-2`) → 브라우저에서 Builder ID → nook-prod. **Builder ID 페더레이션 세션에서도 동작**(→ `AccountFullAccessRole`). 장기 액세스 키 없음, 단기 자격 증명(최대 12시간) |
| 21 | **IAM 역할 `nook-readonly`** | nook-prod IAM → 역할 생성 → AWS 계정 "이 계정", MFA 필요 **해제**(페더레이션 세션에 MFA 표시 없음) → AWS 관리형-직무 **`ReadOnlyAccess`**. CLI 프로필 `nook-readonly` = `role_arn` + `source_profile nook-prod` + `region ap-northeast-2`. 확인: 역할 전환 ✅, `ec2 create-security-group --dry-run` → `UnauthorizedOperation`(역할 정책에서 거부) ✅ |

---

## 5. 조직 정책(SCP) 현황

SCP = 계정 안에서 어떤 권한을 받아도 넘을 수 없는 **상한선**. **관리 계정에는 적용되지 않는다.** IAM 평가 순서: 명시적 Deny가 하나라도 있으면 거부 → Allow가 있으면 허용 → 없으면 기본 거부.

| 정책 | 연결 | 내용 | 조치 |
|---|---|---|---|
| `FullAWSAccess` | 기본 | 모든 작업 허용(상한) | 유지 |
| `AdvancedModeRegionRestrictionSecurityControlPolicy` | **Root** | `RegionFloor`: 허용 목록 밖 리전의 모든 작업 거부. `UsEast1Partitional`: 버지니아는 글로벌 서비스만. `UsWest2Partitional`: 오리건은 Identity Center 등 일부만 | **`RegionFloor`에 서울 추가** → **시드니 제거(2026-11-09)**. 현재 허용: `ap-northeast-2`, `unspecified`, `us-east-1`, `us-west-2`. 제거 전 시드니 자원 0개 조회(EC2·EBS·EIP·SES·태그 API·S3), 제거 후 시드니 `ec2 describe-instances` → `UnauthorizedOperation … explicit deny in a service control policy` ✅, 서울 0건 정상 ✅. `NotAction` bedrock 예외는 AWS 것이라 유지 |
| `ManagedAccountSecurityControlPolicy` | Root | `BlockOrgEscape`: 멤버 계정의 자체 해지·조직 탈퇴·Identity Center 인스턴스 생성 금지. `ProtectManagedRoles`: `role/managed/*` 변경 금지 | 유지(우리 계획을 막지 않음) |
| `BudgetsSpendLimitDeny*` 4개 | **없음** | 사라진 "지출 한도"의 구현체(예측 지출이 한도에 닿으면 Budgets가 붙이던 SCP) | 그대로 둠(Budgets 서비스 관리) |

⚠️ `RegionFloor`는 AWS가 만든 정책이다. 서울이 다시 막히거나 **시드니가 다시 목록에 생기면**(AWS가 덮어쓸 가능성) 이 정책부터 본다.

**리전은 두 곳에서 따로 관리한다** — 바꿀 때 둘 다 고친다(시드니 정리 때 포함):

| 층 | 위치 | 성격 |
|---|---|---|
| SCP `RegionFloor` | Management → Organizations → 정책 | **보안**(API 차단) |
| 표시되는 리전 | 각 계정 세션 → 통합 설정 → 계정 설정 | **편의**(콘솔 메뉴만 숨김, URL 직접 접근 시 안내만) |

표시 목록에서 버지니아를 빼면 IAM·Billing·Support 콘솔을 못 연다(편집 화면 경고). 편집 폼이 현재 메뉴와 다르게 "1/17"로 열렸으니 저장 전 선택 수와 경고 사라짐을 확인한다.

---

## 6. 보안 상태 요약

| 경로·항목 | 보호 |
|---|---|
| 루트(관리 계정) | 긴 무작위 비밀번호(Builder ID와 다름) + MFA 2개 + 액세스 키 없음 + 평소 미사용 |
| Builder ID(관리 계정까지 접근 가능) | 비밀번호 + MFA 2개 + 복구 이메일 |
| 비밀번호 재설정 경로 | Gmail 계정 → **Gmail 2단계 인증 필수**(마지막 방어선). 복구 이메일이 같은 Gmail 별칭이라 독립 경로 아님 → 추후 다른 메일 서비스로 |
| 비용 | 지출 한도(자동 차단) **없음** → Budgets 알림 2개 |
| 감사 | CloudTrail 조직 트레일(멤버 계정이 끄거나 지울 수 없음) |
| 리전 | SCP로 서울·필수 리전 외 차단 |
| S3 공개 | nook-prod 계정 단위 퍼블릭 액세스 차단 4개 활성 → 이미지 공개는 버킷 공개가 아니라 CloudFront + OAC로 |

---

## 7. 남은 일

- [x] 콘솔 서울 리전 메뉴 — 원인은 통합 설정 "표시되는 리전"(4절 17단계). Support 불필요
- [x] S3 계정 퍼블릭 액세스 차단 (nook-prod) — 4개 모두 활성(2026-11-07, 4절 19단계)
- [ ] Identity Center(Delegated Admin, `us-east-1`): 일상 사용자 + 권한 세트(관리자, ReadOnly)
  - 확인(2026-11-07): 자격 증명 소스 = **AWS Builder ID**(프로비저닝 "직접"), 사용자 = Builder ID 1명(이미 등록 → 새 사용자 불필요), 그룹 = 계정별 `<계정ID>-AdministratorAccess` 2개(자동 생성), 조직 인스턴스·위임 관리자 등록됨. **"다중 계정 권한"(AWS 계정·권한 세트) 메뉴 없음**, `#/permissionSets` 주소도 대시보드로 되돌아감 → 이 인스턴스에서 권한 세트로 ReadOnly를 만들 수 없음 → **`aws configure sso` 계획도 재검토**. 자격 증명 소스는 바꾸지 않는다(로그인 경로가 깨질 위험)
  - **대안 채택**: Amazon Q는 "계정 인스턴스라서 → 인스턴스 삭제 후 재생성"을 권했으나 화면은 `인스턴스 유형: 조직 인스턴스` → 전제 오류·파괴적이라 **기각**. 대신 nook-prod IAM 역할 + `aws login`(4절 20·21단계)
- [x] ~~로컬 AWS CLI 설치 + `aws configure sso`~~ → CLI 2.39.5 + **`aws login`** + `nook-readonly` 역할 프로필. Claude 읽기 검증 완료(8절)
- [x] (선택) 콘솔 계정 이름 `nook-prod`로 변경 — 12단계 프로젝트 이름 변경으로 이미 반영됨. 세션 표시 이름만 옛 이름(며칠 지속 시 Support 문의에 함께)
- [ ] (선택) 관리 계정 콘솔 기본 리전을 서울로(Unified Settings)
- [ ] Builder ID 복구 이메일을 다른 메일 서비스로 교체
- [ ] 같은 Gmail에 예전 Builder ID가 있는지 확인(복구 이메일 "이미 사용 중" 단서) — 있으면 MFA 또는 삭제
- [x] 도메인 구매(5단계 전) — `fillyournookwithstars.com`(2026-11-09, dep-plan 결정 5). SES 서울 도메인 인증·DMARC `quarantine`·운영 승인 요청 제출·카카오 SDK 도메인 추가까지 완료
- [ ] **비용 알림 안전망** (2026-11-08 추가 — 지출 한도가 없어 알림만 남았으므로 "알림이 실제로 도착하는지"까지 확인)
  - [ ] Budgets 알림 메일 도착 확인: 첫 과금(5단계 이후) 때 `nook-zero-spend` 메일이 Gmail **받은편지함**에 오는지(스팸·필터 확인)
  - [ ] **비용 이상 탐지(Cost Anomaly Detection)** 켜기: Management → Billing → 비용 이상 탐지 → 모니터(AWS 서비스) + 이메일 구독. 무료. 키 유출 같은 급격한 지출 대비
  - [ ] 캘린더 알림: ① **크레딧 만료일**(Billing → 크레딧에서 날짜 확인 — 가입 시 $100·182일. 만료 뒤부터 실제 청구가 체감됨) ② **도메인 갱신일**(구매 후, 갱신 2주 전)
- [x] nook-prod 루트: `AccountMFAEnabled 0`(9절)이었으나 Management → IAM → 루트 액세스 관리에서 nook-prod 루트 자격 증명 **"없음"** 확인(2026-11-07) → 루트로 로그인할 경로 자체가 없어 MFA 불필요
- [x] **7단계(개인정보) 전**: S3에 사용자 데이터가 들어가기 전 `nook-readonly`를 `ReadOnlyAccess` → `ViewOnlyAccess + SecurityAudit`로 축소(데이터 읽기 제거) — 2026-11-09 사용자 콘솔(연결 먼저 → `ReadOnlyAccess` 제거). 확인: `sts get-caller-identity` = `assumed-role/nook-readonly/…`, 서울 `ec2 describe-instances` 0건 조회 성공
- [x] 시드니 정리 시 SCP `RegionFloor`와 "표시되는 리전" **둘 다** 수정 — 2026-11-09. SCP는 5절 표, nook-prod 표시 리전은 버지니아·오리건·서울 3개(사용자 확인). 계기: 같은 날 SES 이메일 자격 증명을 시드니에 잘못 만듦(콘솔이 마지막 리전 기억)

---

## 8. 배운 점 (사실 기반, 면접용)

- **다중 계정 구조**: 권한이 다른 일(돈·사람·서비스)은 계정을 나눠 한 곳이 털려도 피해가 그 계정에서 멈추게 한다. 만들 때는 위→아래, 지울 때는 아래→위(멤버 계정을 모두 닫아야 관리 계정을 닫을 수 있음)
- **로그인 경로를 센다**: 같은 이메일에 Builder ID·루트 두 경로 + 재설정 메일(Gmail). 가장 약한 경로가 전체 수준
- **단계적 인증(step-up)**: MFA를 추가하려면 MFA로 로그인한 세션이 필요 — 비밀번호만 훔친 공격자가 자기 MFA를 심는 것을 막는다
- **"화면이 보인다 ≠ 권한이 있다"**: 콘솔 리전 표시와 API 권한은 다른 층. CloudShell/CLI로 API를 직접 호출해 층을 가른다
- **차단 vs 알림**: 지출 한도(차단)는 비용을 확실히 막지만 서비스를 멈춘다. Budgets(알림)는 서비스를 살리지만 사람이 대응해야 한다
- **설정의 주인**: 상위 시스템이 관리하는 리소스(SCP)를 직접 고치면 덮어써질 수 있다 → 공식 방법(`RegionFloor` 수정)을 문서로 확인하고, 다시 막히면 여기부터 본다
- **AI 답변은 관찰 사실과 대조한다**: Amazon Q의 "계정 인스턴스" 진단은 화면의 `조직 인스턴스` 한 줄로 기각됐다. 파괴적 조치(삭제·재생성)를 권하면 그 전제부터 확인한다
- **"읽기 전용"도 데이터를 읽는다**: `ReadOnlyAccess`는 S3 객체 내용까지 읽는다. 설정 조회(`ViewOnlyAccess`·`SecurityAudit`)와 데이터 조회를 구분한다
- **거부도 층을 본다**: 같은 "거부"라도 `no identity-based policy allows`(IAM 역할) vs `explicit deny in a service control policy`(SCP)로 어느 층이 막았는지 메시지가 알려 준다

---

## 9. CLI 읽기 검증 (2026-11-07, Claude, 프로필 `nook-readonly`)

| 항목 | 명령 | 결과 |
|---|---|---|
| 신원 | `sts get-caller-identity` | `assumed-role/nook-readonly/…` ✅ |
| S3 계정 퍼블릭 차단 | `s3control get-public-access-block` | 4개 모두 `true` ✅ |
| S3 버킷 | `s3api list-buckets` | `[]`(과금 대상 없음) ✅ |
| 서울 EC2 (SCP 허용) | `ec2 describe-instances --region ap-northeast-2` | `[]` ✅ |
| 도쿄 EC2 (SCP 차단) | `ec2 describe-instances --region ap-northeast-1` | `explicit deny in a service control policy` ✅ |
| CloudTrail | `cloudtrail describe-trails` | `nook-org-trail`: 서울 홈, 조직, 다중 리전, 로그 검증 켬 ✅ |
| IAM 역할 | `iam list-roles`(서비스 역할 제외) | `managed/` 3개(AccountFullAccessRole 등, SCP `ProtectManagedRoles` 보호) + `nook-readonly` ✅ |
| IAM 사용자·키 | `iam list-users`, `get-account-summary` | 사용자 0, 루트 액세스 키 0 ✅ · 루트 MFA 0 → 루트 자격 증명 자체가 "없음"(루트 액세스 관리) ✅ |
| 쓰기 차단 | `iam create-user`, `ec2 create-security-group --dry-run` | `no identity-based policy allows` ✅ |

nook-prod에서 볼 수 없는 것(관리 계정 소관): Budgets 2개, SCP 본문, 관리 계정 루트 MFA → 관리 계정 콘솔에서 확인(4절 기록 기준).
