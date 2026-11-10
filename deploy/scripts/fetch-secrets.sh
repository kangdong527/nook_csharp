#!/usr/bin/env bash
# 운영 .env 만들기 — 설정값(deploy/prod.env) + 비밀값(SSM SecureString /nook/prod/*) → ENV_FILE(권한 600). DEP 6단계, Claude 작성(2026-11-10).
#
# 사용(리포 루트 = EC2 의 /opt/nook): deploy/scripts/fetch-secrets.sh   — deploy.sh 가 매 배포 시작에 부른다
#   자격 증명: 인스턴스 역할 nook-ec2(ssm:GetParameters, /nook/prod/* 한정). KMS 는 AWS 관리형 aws/ssm(결정 3) → kms 권한 불필요
#
# 비밀값 규칙(dep-plan 6단계): 값은 변수와 ENV_FILE 에만. echo·로그·명령줄 인자로 내보내지 않는다 → set -x 금지
# 실패하면 기존 ENV_FILE 을 건드리지 않는다(임시 파일에 다 쓴 뒤 mv 로 교체).
set -euo pipefail

ENV_FILE="${ENV_FILE:-/opt/nook/.env}"
CONFIG_FILE="${CONFIG_FILE:-deploy/prod.env}"
REGION="${AWS_REGION:-ap-northeast-2}"
PREFIX="/nook/prod"                              # 결정 1: /nook/prod/<환경변수 이름>
SECRETS=(POSTGRES_PASSWORD JWT_SIGNING_KEY)      # 결정 2: 비밀값만. GetParameters 는 한 번에 10개까지

names=()
for key in "${SECRETS[@]}"; do names+=("$PREFIX/$key"); done

# GetCallerIdentity 는 권한 없이 항상 허용된다 → 계정 ID 를 리포에 적지 않아도 된다
account_id="$(aws sts get-caller-identity --query Account --output text)"

# 없는 이름은 오류가 아니라 InvalidParameters 로 돌아온다(종료 코드 0) → 개수로 확인
pairs="$(aws ssm get-parameters --region "$REGION" --with-decryption \
  --names "${names[@]}" --query 'Parameters[].[Name,Value]' --output text)"
got="$(printf '%s\n' "$pairs" | grep -c . || true)"
if (( got != ${#SECRETS[@]} )); then
  missing="$(aws ssm get-parameters --region "$REGION" --names "${names[@]}" \
    --query 'InvalidParameters' --output text)"
  echo "SSM 비밀값 ${got}/${#SECRETS[@]}개만 읽음. 없는 이름: ${missing:-?}" >&2
  exit 1
fi

tmp="$(mktemp "${ENV_FILE}.XXXXXX")"             # mktemp 는 600 으로 만든다
trap 'rm -f "$tmp"' EXIT
{
  echo "# fetch-secrets.sh 가 생성($(date -u +%FT%TZ)). 직접 고치지 말 것 — 설정은 $CONFIG_FILE, 비밀값은 SSM $PREFIX"
  sed "s/<ACCOUNT_ID>/${account_id}/g" "$CONFIG_FILE"
  while IFS=$'\t' read -r name value; do
    printf '%s=%s\n' "${name##*/}" "$value"
  done < <(printf '%s\n' "$pairs")
} > "$tmp"
chmod 600 "$tmp"
mv "$tmp" "$ENV_FILE"
trap - EXIT
echo "생성: $ENV_FILE (설정 $CONFIG_FILE + SSM 비밀값 ${#SECRETS[@]}개)"
