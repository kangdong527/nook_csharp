# 비밀값 교체·침해 격리 런북

DEP 10단계 Pillar(Incident response). Claude 작성(2026-11-13), 결정(사용자): 문서 + JWT만 운영 리허설, DB 비밀번호는 로컬 리허설.

## 0. 언제 여는가

| 상황 | 할 일 |
|---|---|
| 비밀값이 리포·이미지·로그·채팅·화면 공유에 노출됐다(의심 포함) | **즉시 교체** — 노출된 값은 지워도 되돌릴 수 없다(git·이미지는 추가만 하는 저장소) |
| gitleaks·Trivy CI가 실제 비밀값을 잡았다 | 교체 → 그다음 커밋·이미지 정리. `.gitleaksignore`에 넣지 않는다 |
| 정기 교체 | 지금은 하지 않음(키 2개·운영자 1명). 운영자가 바뀌면 교체 |

비밀값은 2개뿐이다(dep-plan 6단계 결정 2). 리포에는 이름만 있고 값은 SSM SecureString에만 있다.

| 이름 | SSM | 쓰는 곳 | 바꾸면 |
|---|---|---|---|
| `JWT_SIGNING_KEY` | `/nook/prod/JWT_SIGNING_KEY` | api `Jwt__SigningKey` | 발급된 액세스 토큰 전부 무효 → **전원 재로그인**. 중단 없음 |
| `POSTGRES_PASSWORD` | `/nook/prod/POSTGRES_PASSWORD` | db 초기화·api·migrate 연결 문자열 | **순서가 틀리면 api가 DB 접속 실패**(아래 B) |

## 공통: 재배포(같은 sha)

값을 바꾼 뒤 지금 서비스 중인 sha를 다시 배포한다. `deploy.sh`가 `fetch-secrets.sh`로 `/opt/nook/.env`를 새로 만들고, compose는 **환경변수가 바뀐 컨테이너만** 다시 만든다. 실패하면 직전 태그로 자동 롤백하지만 롤백도 같은(새) 비밀값을 쓴다 — 비밀값 문제는 롤백으로 안 풀린다.

```powershell
# 로컬 PC, nook-prod. TAG = 서버 /opt/nook/state/current-tag
$TAG = "<sha>"
$cid = aws ssm send-command --document-name nook-deploy --instance-ids i-02eed84198de4f971 `
  --parameters "ImageTag=$TAG" --comment "secret rotation" --profile nook-prod --region ap-northeast-2 `
  --query Command.CommandId --output text
aws ssm get-command-invocation --command-id $cid --instance-id i-02eed84198de4f971 `
  --profile nook-prod --region ap-northeast-2 --query "[Status,StandardOutputContent]" --output text
```
`Success` + `배포 성공: <sha>`가 나올 때까지 두 번째 명령을 다시 실행한다(보통 1분 안).

**작업 중 `main` 병합 금지** — 자동 배포가 끼어들면 순서가 섞인다.

## A. JWT 서명 키

실행 위치: 1·2 = **로컬 PC PowerShell 한 창**(변수 `$k`·`$TAG`·`$cid`가 그 창에만 있다), 서버 접속 없음. 3 = 브라우저.

1. **새 값 만들고 바로 SSM에 넣기**(로컬 PowerShell, nook-prod — 쓰기 권한은 nook-prod에만, 인스턴스 역할은 읽기만. 값을 화면·채팅에 내지 않는다. 히스토리엔 `$k`만 남는다)
   ```powershell
   # Windows PowerShell 5.1(.NET Framework)엔 RandomNumberGenerator::Fill 이 없다(실측) → Create().GetBytes
   $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); $k = [Convert]::ToBase64String($b)
   $k.Length   # 64
   aws ssm put-parameter --name /nook/prod/JWT_SIGNING_KEY --type SecureString --value $k --overwrite --profile nook-prod --region ap-northeast-2
   Remove-Variable k, b
   ```
   출력의 `Version`이 1 올라간다.
2. **재배포**(공통)
3. **확인**: 교체 전에 로그인해 둔 브라우저에서 새로고침 → 로그인 화면으로 가야 한다(옛 토큰 거절). 다시 로그인하면 정상. 실제 화면·응답은 실행 기록에 적는다

## B. DB 비밀번호

함정: postgres 이미지는 `POSTGRES_PASSWORD`를 **빈 볼륨에 DB를 처음 만들 때만** 읽는다. 실제 비밀번호는 볼륨 안(`pg_authid`)에 있다 → SSM만 바꾸고 재배포하면 api·migrate가 새 값으로 접속을 시도해 실패한다.

순서 = **SSM → ALTER → 즉시 재배포**. SSM 값은 다음 `fetch-secrets` 전까지 효과가 없어서 먼저 바꿔도 안전하다. 어긋나는 구간은 ALTER 부터 재배포 끝까지(수십 초). 그동안 api의 **이미 열린** 풀 연결은 계속 동작하고 새 연결만 실패한다.

실행 위치: 1·3 = **로컬 PC PowerShell**, 2 = **서버(Session Manager)**, 4 = 브라우저.

1. **새 값 → SSM**(로컬 PowerShell): A-1과 같은 방법, 이름만 `/nook/prod/POSTGRES_PASSWORD`. 길이는 지금과 같게 32(`byte[] 24`). 연결 문자열에 들어가므로 Base64의 `+`·`/`·`=`는 피한다 → `[Convert]::ToBase64String($b).Replace('+','A').Replace('/','B').TrimEnd('=')`
2. **서버(Session Manager)**: 새 값을 받아 DB 안 비밀번호 변경
   ```bash
   cd /opt/nook/releases/$(cat /opt/nook/state/current-tag) && sudo deploy/scripts/fetch-secrets.sh
   sudo bash -c 'set -a; . /opt/nook/.env; docker exec -i nook-deploy-db-1 sh -c "psql -U \"\$POSTGRES_USER\" -d \"\$POSTGRES_DB\" -v ON_ERROR_STOP=1 -q -v pw=\"$POSTGRES_PASSWORD\"" <<< "ALTER USER CURRENT_USER PASSWORD :'"'"'pw'"'"';"'
   ```
   값을 표준 입력의 psql 변수(`:'pw'`)로 넘긴다 — 셸 히스토리·psql 히스토리에 남지 않는다. 남는 위험: 문장이 **오류**를 내면 PostgreSQL 로그에 문장 전체가 찍힐 수 있다(그러면 그 값을 버리고 1부터 다시)
3. **즉시 재배포**(공통) — db·api·migrate 컨테이너가 새 `.env`로 다시 만들어진다
4. **확인**: `배포 성공`, 사이트 로그인·지도. 백업 타이머는 컨테이너 안 로컬 소켓(비밀번호 불필요)이라 영향 없음

**되돌리기**: 3이 실패하면 SSM을 직전 버전 값으로 돌릴 수 없다(SecureString 이력은 남지만 운영자가 값을 모른다) → 2를 새 값으로 다시 실행해 DB와 SSM을 맞춘다. DB 접속 자체가 안 되면 컨테이너 안 로컬 소켓은 비밀번호 없이 들어가진다.

## C. 침해 의심 시 격리 (교체보다 먼저)

언제: GuardDuty 탐지(Backup check 워크플로가 실패로 알림), 모르는 프로세스·아웃바운드, CloudTrail에 모르는 호출.
순서 = **증거 보존 → 인바운드 차단 → 조사 → 비밀값 교체(A·B) → 복구**. 인스턴스를 **종료하지 않는다**(디스크·메모리 증거가 사라짐).

실행 위치: 전부 **로컬 PC PowerShell(nook-prod)**. SG 인바운드를 다 막아도 Session Manager는 된다(에이전트가 바깥으로 나가는 연결 — 인바운드 불필요).

1. **증거 보존 스냅샷**(DLM 7개 회전에 밀려 지워지지 않게 수동으로)
   ```powershell
   aws ec2 create-snapshot --volume-id vol-09c74d62acbb5a4fa --description "incident $(Get-Date -Format yyyyMMdd-HHmm)" `
     --tag-specifications "ResourceType=snapshot,Tags=[{Key=Name,Value=nook-incident}]" --profile nook-prod --region ap-northeast-2
   ```
2. **인바운드 전부 차단**(SG `nook-web` = `sg-0f26f4ece6c8d3761`). 지금 규칙을 파일로 남겨 두고 그 파일로 지운다 → 복구 때 같은 파일로 되돌린다
   ```powershell
   $sg = "sg-0f26f4ece6c8d3761"; $f = "$env:TEMP\nook-sg-ingress-before.json"
   aws ec2 describe-security-groups --group-ids $sg --query "SecurityGroups[0].IpPermissions" --output json --profile nook-prod --region ap-northeast-2 | Out-File -Encoding ascii $f
   aws ec2 revoke-security-group-ingress --group-id $sg --ip-permissions "file://$f" --profile nook-prod --region ap-northeast-2
   aws ec2 describe-security-groups --group-ids $sg --query "length(SecurityGroups[0].IpPermissions)" --profile nook-prod --region ap-northeast-2   # 0
   ```
   사이트는 이때부터 접속 불가(의도). 아웃바운드는 남긴다 — 지우면 SSM 접속·ECR도 끊겨 조사·복구를 못 한다. 데이터 유출이 진행 중이라 판단되면 그때 아웃바운드도 정한다
3. **조사**: CloudTrail(누가 어떤 키로), GuardDuty 결과 상세(콘솔), 서버 `docker ps -a`·`sudo ss -tunp`·`sudo last`
4. **교체**: A(JWT)·B(DB). 인스턴스 역할 자격 증명이 의심되면 역할 세션 무효화(IAM → 역할 `nook-ec2` → **세션 취소**)
5. **복구**: 오염이 의심되면 런북 `restore-runbook.md` §B(깨끗한 스냅샷에서 새로) / 아니면 인바운드 되돌리기
   ```powershell
   aws ec2 authorize-security-group-ingress --group-id $sg --ip-permissions "file://$f" --profile nook-prod --region ap-northeast-2
   ```

**미실행**(2026-11-13 문서만): 2는 사이트 중단을 만든다 — 실행해 보려면 공개 전(지금처럼 443이 집 IP만일 때)이 부담이 작다.

## 실행 기록

| 날짜 | 대상 | 환경 | 결과 |
|---|---|---|---|
| 2026-11-13 | JWT 서명 키(리허설) | **운영** `239a0a3` | A-1 SSM `Version 2`(값 출력 없음) → 같은 sha 재배포 `InProgress`→`Success`·`배포 성공` → 교체 전 로그인해 둔 브라우저 새로고침 = **로그인 풀림**(옛 토큰 거절), 재로그인 정상. 중단 없음. 실측: PowerShell 5.1엔 `RandomNumberGenerator::Fill` 없음 |
| 2026-11-13 | DB 비밀번호(리허설) | **로컬** nook-deploy compose(db·migrate·api) | ① 기준선 ready 200 ② **함정 재현**: env만 새 값 → `up` → migrate `password authentication failed for user "nook"`·exit 1, api `Created`(미기동), ready 000 ③ B-2 명령 그대로(경로만 로컬) ALTER exit 0 → `up` → ready 200. psql 히스토리 파일 없음·DB 로그 `alter` 0건 ④ 되돌리기: 원래 값 ALTER → 원래 `.env`로 `up` → 200. 발견: 로컬 `deploy/.env`는 CRLF(17줄) → bash `. .env`로 읽으면 값 끝에 `\r`(compose는 처리). 서버 `.env`는 `fetch-secrets.sh`가 리눅스에서 만들어 LF |

