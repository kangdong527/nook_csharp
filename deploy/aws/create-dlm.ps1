# EBS 스냅샷 DLM 정책 — DEP 9단계. Claude 작성(2026-11-13), 실행은 사용자(쓰기 권한 프로필 nook-prod).
#   powershell -ExecutionPolicy Bypass -File deploy/aws/create-dlm.ps1
# 결정(사용자, 2026-11-13): 매일 · 7개 보존 · 05:00 KST(= 20:00 UTC, pg_dump 04:00 뒤). DLM 시각은 UTC, 지정 시각부터 1시간 안에 시작.
# 대상은 태그 nook-dlm=daily 가 붙은 볼륨 — 인스턴스에 붙은 루트 볼륨에 이 스크립트가 태그를 단다.
# 다시 실행해도 안전하다: 역할·태그는 있으면 그대로, 정책은 같은 설명(description)이 있으면 update 로 다시 맞춘다.
# 출력 문구는 영어 — Windows PowerShell 5.1 은 BOM 없는 UTF-8 스크립트의 한글 문자열을 깨뜨린다.
$aws = 'C:\Program Files\Amazon\AWSCLIV2\aws.exe'
$common = @('--profile', 'nook-prod', '--region', 'ap-northeast-2')
$instance = 'i-02eed84198de4f971'
$desc = 'nook-prod daily EBS snapshot'
$details = 'file://deploy/aws/dlm-policy.json'

# 1. DLM 기본 역할(AWS 관리형 정책 AWSDataLifecycleManagerServiceRole) — 계정에 1번
$roleArn = & $aws iam get-role --profile nook-prod --role-name AWSDataLifecycleManagerDefaultRole --query Role.Arn --output text 2>$null
if ($LASTEXITCODE -ne 0) {
    & $aws dlm create-default-role @common --resource-type snapshot | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Output 'FAILED create-default-role'; exit 1 }
    $roleArn = & $aws iam get-role --profile nook-prod --role-name AWSDataLifecycleManagerDefaultRole --query Role.Arn --output text
}
Write-Output "role: $roleArn"

# 2. 인스턴스 루트 볼륨에 대상 태그
$vol = & $aws ec2 describe-volumes @common --filters "Name=attachment.instance-id,Values=$instance" --query 'Volumes[0].VolumeId' --output text
if ($LASTEXITCODE -ne 0 -or $vol -notlike 'vol-*') { Write-Output "FAILED find volume: $vol"; exit 1 }
& $aws ec2 create-tags @common --resources $vol --tags Key=nook-dlm,Value=daily Key=Name,Value=nook-prod
if ($LASTEXITCODE -ne 0) { Write-Output 'FAILED create-tags'; exit 1 }
Write-Output "volume tagged: $vol"

# 3. 정책 만들기 또는 다시 맞추기
$existing = & $aws dlm get-lifecycle-policies @common --query "Policies[?Description=='$desc'].PolicyId | [0]" --output text
if ($existing -like 'policy-*') {
    & $aws dlm update-lifecycle-policy @common --policy-id $existing --execution-role-arn $roleArn --state ENABLED --policy-details $details
    if ($LASTEXITCODE -ne 0) { Write-Output 'FAILED update-lifecycle-policy'; exit 1 }
    Write-Output "policy updated: $existing"
} else {
    & $aws dlm create-lifecycle-policy @common --execution-role-arn $roleArn --description $desc --state ENABLED --policy-details $details --tags Name=nook-prod
    if ($LASTEXITCODE -ne 0) { Write-Output 'FAILED create-lifecycle-policy'; exit 1 }
}
Write-Output 'OK: daily 20:00 UTC (05:00 KST), keep 7'
