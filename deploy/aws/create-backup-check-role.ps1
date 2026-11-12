# 백업 데드맨 스위치용 GitHub 역할 — DEP 10단계. Claude 작성(2026-11-13), 실행은 사용자(쓰기 프로필 nook-prod).
#   powershell -ExecutionPolicy Bypass -File deploy/aws/create-backup-check-role.ps1
# 역할 nook-github-backup-check: 신뢰 = 배포 역할과 같은 조건(main 브랜치만, deploy/iam/nook-github-deploy-trust.json 재사용)
#                               권한 = deploy/iam/nook-github-backup-check-policy.json (조회만)
# OIDC 공급자는 create-deploy-identity.ps1 이 이미 만들었다. 다시 실행해도 안전(있으면 정책만 다시 맞춤).
# 끝나면 출력된 ARN 을 리포 변수 AWS_BACKUP_CHECK_ROLE_ARN 으로 등록한다.
$aws = 'C:\Program Files\Amazon\AWSCLIV2\aws.exe'
$env:AWS_CLI_FILE_ENCODING = 'UTF-8'
$p = @('--profile', 'nook-prod')
$role = 'nook-github-backup-check'
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
    $trust = Render 'deploy/iam/nook-github-deploy-trust.json'
    & $aws iam get-role @p --role-name $role --query Role.RoleName --output text 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & $aws iam create-role @p --role-name $role --assume-role-policy-document $trust `
            --description 'GitHub Actions scheduled backup freshness check for nook (read-only)' `
            --max-session-duration 3600 --tags Key=Name,Value=nook-prod --output text | Out-Null
        Check "create-role $role"
        Write-Output "role ${role}: created"
    } else {
        & $aws iam update-assume-role-policy @p --role-name $role --policy-document $trust | Out-Null
        Check 'update-assume-role-policy'
        Write-Output "role ${role}: trust policy updated"
    }
    & $aws iam put-role-policy @p --role-name $role --policy-name "$role-app" `
        --policy-document (Render 'deploy/iam/nook-github-backup-check-policy.json') | Out-Null
    Check "put-role-policy $role-app"
    Write-Output "role ${role}: inline policy applied"
    Write-Output "ARN (repo variable AWS_BACKUP_CHECK_ROLE_ARN): arn:aws:iam::${acct}:role/$role"
} finally {
    Remove-Item -Recurse -Force $tmp
}
