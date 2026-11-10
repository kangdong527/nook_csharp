#!/usr/bin/env bash
# 배포 — 새 이미지 태그로 올리고 /health/ready 로 확인, 실패하면 직전 태그로 되돌린다. DEP 8단계, Claude 초안(2026-11-09).
#
# 사용(리포 루트 = EC2 의 /opt/nook): deploy/scripts/deploy.sh <이미지 태그>
#   필요한 환경: IMAGE_REPO(예: <계정>.dkr.ecr.ap-northeast-2.amazonaws.com/nook), 나머지 값은 ENV_FILE(기본 /opt/nook/.env)
#   8단계: GitHub Actions → SSM Run Command 가 이 스크립트를 부른다(SSH 없음, 결정 4b)
#
# 순서: pull → up(migrate 1회성 → api → web) → ready 확인 → 성공이면 태그 기록 / 실패면 직전 태그로 up + 확인 → exit 1
# ⚠️ 롤백은 이미지만 되돌린다. 마이그레이션은 되돌아가지 않는다 → 마이그레이션은 직전 버전 코드와 호환되게(확장 → 전환 → 축소) 쓴다.
set -euo pipefail

TAG="${1:?배포할 이미지 태그(커밋 sha)를 주세요}"
ENV_FILE="${ENV_FILE:-/opt/nook/.env}"
STATE_DIR="${STATE_DIR:-/opt/nook/state}"   # 마지막으로 성공한 태그를 남기는 곳
COMPOSE_FILES=(-f deploy/compose.yaml -f "${COMPOSE_OVERRIDE:-deploy/compose.prod.yaml}")
READY_TIMEOUT="${READY_TIMEOUT:-90}"         # 초. migrate + api 기동 시간을 넉넉히
NETWORK="nook-deploy_backend"
PROBE_IMAGE="curlimages/curl:8.17.0"         # api 이미지(chiseled)엔 curl 이 없다 → backend 망에 잠깐 붙는 curl 컨테이너로 확인(2026-11-09 결정)

mkdir -p "$STATE_DIR"
PREVIOUS="$(cat "$STATE_DIR/current-tag" 2>/dev/null || true)"

compose() { IMAGE_TAG="$1" docker compose "${COMPOSE_FILES[@]}" --env-file "$ENV_FILE" "${@:2}"; }

ready() {
  local deadline=$(( $(date +%s) + READY_TIMEOUT ))
  while (( $(date +%s) < deadline )); do
    if docker run --rm --network "$NETWORK" "$PROBE_IMAGE" -fsS -m 5 http://api:8080/health/ready > /dev/null 2>&1; then
      return 0
    fi
    sleep 3
  done
  return 1
}

up() {  # $1 = 태그. migrate 가 실패하면 api 가 뜨지 않는다(depends_on: service_completed_successfully) → up 이 실패한다
  [[ "${SKIP_PULL:-0}" == 1 ]] || compose "$1" pull   # SKIP_PULL=1: 레지스트리 없이 로컬 이미지로 리허설할 때만
  compose "$1" up -d --no-build --remove-orphans
}

# 6단계: 매 배포마다 .env 를 SSM + deploy/prod.env 로 다시 만든다(비밀값 교체·설정 변경이 다음 배포에 반영).
# 실패하면 기존 .env 그대로 두고 배포하지 않는다. SKIP_SECRETS=1: 로컬 리허설(손으로 만든 ENV_FILE)일 때만
[[ "${SKIP_SECRETS:-0}" == 1 ]] || ENV_FILE="$ENV_FILE" deploy/scripts/fetch-secrets.sh

echo "배포: ${PREVIOUS:-(없음)} → $TAG"
if up "$TAG" && ready; then
  echo "$TAG" > "$STATE_DIR/current-tag"
  echo "배포 성공: $TAG"
  exit 0
fi

echo "배포 실패: $TAG — /health/ready 가 ${READY_TIMEOUT}s 안에 200 이 아님" >&2
compose "$TAG" logs --tail 50 migrate api >&2 || true

if [[ -z "$PREVIOUS" ]]; then
  echo "되돌릴 직전 태그가 없습니다(첫 배포). 수동 확인이 필요합니다." >&2
  exit 1
fi

echo "롤백: $PREVIOUS" >&2
if up "$PREVIOUS" && ready; then
  echo "롤백 성공: $PREVIOUS 로 서비스 중. 실패한 태그 $TAG 를 확인하세요." >&2
else
  echo "롤백도 실패: 서비스가 내려가 있을 수 있습니다. 즉시 수동 확인." >&2
fi
exit 1
