# ECR 저장소 4개 만들기 — DEP 8단계 결정 D8-3. Claude 작성(2026-11-10), 실행은 사용자(쓰기 권한 프로필 nook-prod).
#   powershell -ExecutionPolicy Bypass -File deploy/aws/create-ecr.ps1
# 다시 실행해도 안전하다: 있는 저장소는 만들지 않고 설정(불변 태그·스캔·수명 주기)만 다시 맞춘다.
# 출력 문구는 영어 — Windows PowerShell 5.1 은 BOM 없는 UTF-8 스크립트의 한글 문자열을 깨뜨린다.
# $ErrorActionPreference='Stop' 을 쓰지 않는다 — 5.1 에서 네이티브 명령의 stderr 리디렉션이 종료 오류가 된다. 종료 코드로 판단한다.
$aws = 'C:\Program Files\Amazon\AWSCLIV2\aws.exe'
$common = @('--profile', 'nook-prod', '--region', 'ap-northeast-2')
$lifecycle = 'file://deploy/aws/ecr-lifecycle.json'

foreach ($repo in 'nook-api', 'nook-web', 'nook-migrate', 'nook-deploy') {
    & $aws ecr describe-repositories @common --repository-names $repo --query 'repositories[0].repositoryName' --output text 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & $aws ecr create-repository @common --repository-name $repo `
            --image-tag-mutability IMMUTABLE `
            --image-scanning-configuration scanOnPush=true `
            --encryption-configuration encryptionType=AES256 `
            --tags Key=Name,Value=nook-prod `
            --query repository.repositoryName --output text
        if ($LASTEXITCODE -ne 0) { Write-Output "FAILED create-repository: $repo"; exit 1 }
    } else {
        & $aws ecr put-image-tag-mutability @common --repository-name $repo --image-tag-mutability IMMUTABLE | Out-Null
        & $aws ecr put-image-scanning-configuration @common --repository-name $repo --image-scanning-configuration scanOnPush=true | Out-Null
        Write-Output "$repo (exists - settings re-applied)"
    }
    & $aws ecr put-lifecycle-policy @common --repository-name $repo --lifecycle-policy-text $lifecycle | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Output "FAILED put-lifecycle-policy: $repo"; exit 1 }
}
Write-Output 'OK: 4 repositories, immutable tags, scan on push, lifecycle policy'
