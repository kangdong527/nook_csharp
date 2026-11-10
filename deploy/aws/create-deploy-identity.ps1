# DEP 8단계 배포 경로의 AWS 쪽 — Claude 작성(2026-11-10), 실행은 사용자(쓰기 프로필 nook-prod).
#   powershell -ExecutionPolicy Bypass -File deploy/aws/create-deploy-identity.ps1
# 하는 일(다시 실행해도 안전 — 있으면 갱신):
#   1) GitHub OIDC 공급자 token.actions.githubusercontent.com (aud sts.amazonaws.com)
#   2) 역할 nook-github-deploy: 신뢰 = deploy/iam/nook-github-deploy-trust.json, 인라인 = nook-github-deploy-policy.json
#   3) SSM 명령 문서 nook-deploy = deploy/aws/ssm-nook-deploy.yaml (새 버전을 기본 버전으로)
#   4) 역할 nook-ec2 인라인 nook-ec2-app = deploy/iam/nook-ec2-inline-policy.json (ECR pull 추가분 반영)
# 리포의 JSON 은 <ACCOUNT_ID> 자리표시자 → 실행 중 계정 ID 로 바꾼 임시 파일을 쓰고 지운다.
# 출력은 영어(5.1 의 BOM 없는 UTF-8 한글 문자열 깨짐), Stop 대신 종료 코드로 판단(5.1 네이티브 stderr).
$aws = 'C:\Program Files\Amazon\AWSCLIV2\aws.exe'
# Windows 의 AWS CLI 는 file:// 을 시스템 인코딩(CP949)으로 읽는다 → 한글 주석이 있는 SSM YAML 이 실패(2026-11-10 실측)
$env:AWS_CLI_FILE_ENCODING = 'UTF-8'
$p = @('--profile', 'nook-prod')
$r = @('--profile', 'nook-prod', '--region', 'ap-northeast-2')
function Check($what) { if ($LASTEXITCODE -ne 0) { Write-Output "FAILED: $what"; exit 1 } }

$acct = (& $aws sts get-caller-identity @p --query Account --output text).Trim(); Check 'sts get-caller-identity'
$tmp = Join-Path $env:TEMP ('nook-iam-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
function Render($src) {
    $dst = Join-Path $tmp (Split-Path $src -Leaf)
    (Get-Content -Raw -Encoding UTF8 $src).Replace('<ACCOUNT_ID>', $acct) | Set-Content -Encoding Ascii -NoNewline $dst
    return 'file://' + ($dst -replace '\\', '/')
}

try {
    # 1) OIDC provider
    $oidcArn = "arn:aws:iam::${acct}:oidc-provider/token.actions.githubusercontent.com"
    & $aws iam get-open-id-connect-provider @p --open-id-connect-provider-arn $oidcArn --query Url --output text 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & $aws iam create-open-id-connect-provider @p --url https://token.actions.githubusercontent.com `
            --client-id-list sts.amazonaws.com --tags Key=Name,Value=nook-prod --output text | Out-Null
        Check 'create-open-id-connect-provider'
        Write-Output 'OIDC provider: created'
    } else { Write-Output 'OIDC provider: exists' }

    # 2) deploy role
    $trust = Render 'deploy/iam/nook-github-deploy-trust.json'
    & $aws iam get-role @p --role-name nook-github-deploy --query Role.RoleName --output text 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & $aws iam create-role @p --role-name nook-github-deploy --assume-role-policy-document $trust `
            --description 'GitHub Actions main branch deploy for nook (ECR push, SSM nook-deploy only)' `
            --max-session-duration 3600 --tags Key=Name,Value=nook-prod --output text | Out-Null
        Check 'create-role nook-github-deploy'
        Write-Output 'role nook-github-deploy: created'
    } else {
        & $aws iam update-assume-role-policy @p --role-name nook-github-deploy --policy-document $trust | Out-Null
        Check 'update-assume-role-policy'
        Write-Output 'role nook-github-deploy: trust policy updated'
    }
    & $aws iam put-role-policy @p --role-name nook-github-deploy --policy-name nook-github-deploy-app `
        --policy-document (Render 'deploy/iam/nook-github-deploy-policy.json') | Out-Null
    Check 'put-role-policy nook-github-deploy-app'
    Write-Output 'role nook-github-deploy: inline policy applied'

    # 3) SSM document
    $doc = 'file://deploy/aws/ssm-nook-deploy.yaml'
    & $aws ssm describe-document @r --name nook-deploy --query Document.Name --output text 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & $aws ssm create-document @r --name nook-deploy --document-type Command --document-format YAML `
            --content $doc --tags Key=Name,Value=nook-prod --output text | Out-Null
        Check 'ssm create-document'
        Write-Output 'SSM document nook-deploy: created'
    } else {
        $ver = & $aws ssm update-document @r --name nook-deploy --document-format YAML --content $doc `
            --document-version '$LATEST' --query DocumentDescription.DocumentVersion --output text 2>&1
        if ($LASTEXITCODE -eq 0) {
            & $aws ssm update-document-default-version @r --name nook-deploy --document-version $ver.Trim() | Out-Null
            Check 'update-document-default-version'
            Write-Output "SSM document nook-deploy: version $($ver.Trim()) is default"
        } elseif ("$ver" -match 'DuplicateDocumentContent') {
            Write-Output 'SSM document nook-deploy: unchanged'
        } else { Write-Output "FAILED: ssm update-document"; exit 1 }
    }

    # 4) nook-ec2 inline policy
    & $aws iam put-role-policy @p --role-name nook-ec2 --policy-name nook-ec2-app `
        --policy-document (Render 'deploy/iam/nook-ec2-inline-policy.json') | Out-Null
    Check 'put-role-policy nook-ec2-app'
    Write-Output 'role nook-ec2: inline policy nook-ec2-app applied'
    Write-Output 'OK'
} finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
