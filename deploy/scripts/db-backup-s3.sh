#!/usr/bin/env bash
# 운영 DB 백업 → S3. DEP 9단계, Claude 작성(2026-11-11). systemd 타이머 nook-backup.timer 가 매일 04:00 KST 에 부른다.
#
# 사용(EC2, 릴리스 폴더 = 리포 루트): deploy/scripts/db-backup-s3.sh
# 결정(사용자): 하루 1회(RPO 24시간) · 보존 매일 7 + 주 4 · 주간분 = KST 일요일 실행분 · 04:00 KST
#
# 권한 경계(dep-plan 5단계): 인스턴스 역할은 백업 버킷 daily/·weekly/ 에 PutObject 만 — 읽기·삭제·목록 없음.
#   → S3 안 복사(CopyObject = 원본 GetObject 필요)는 못 한다. 일요일엔 같은 로컬 파일을 weekly/ 에 한 번 더 올린다.
#   → 오래된 백업 정리는 이 스크립트가 아니라 S3 수명 주기 규칙이 한다(서버가 침해돼도 백업을 못 지운다).
#   → 서버는 S3 백업을 내려받지 못한다 → 복원 리허설·급한 복원에 쓰라고 로컬에 최근 LOCAL_KEEP 개를 남긴다.
set -euo pipefail
umask 077

export AWS_REGION="${AWS_REGION:-ap-northeast-2}"
BACKUP_DIR="${BACKUP_DIR:-/opt/nook/backups}"
LOCAL_KEEP="${LOCAL_KEEP:-3}"

# 버킷 이름은 계정 ID 로 만든다(계정 리전 네임스페이스). sts get-caller-identity 는 권한 없이도 된다
ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
BUCKET="nook-prod-backups-${ACCOUNT}-${AWS_REGION}-an"

NAME="nook-$(date -u +%Y%m%dT%H%M%SZ).dump"
FILE="$BACKUP_DIR/$NAME"
mkdir -p "$BACKUP_DIR"

BACKUP_FILE="$FILE" "$(dirname "$0")/db-backup.sh" "$BACKUP_DIR"

# put-object(한 번에 PUT) — 멀티파트가 아니라 남는 조각이 없다(5GB 까지, 지금 덤프는 수 MB 이하).
# --checksum-algorithm: S3 가 받은 바이트를 검증한다(전송 중 손상이면 업로드 자체가 실패)
put() {
  aws s3api put-object --bucket "$BUCKET" --key "$1" --body "$FILE" \
    --checksum-algorithm SHA256 --query ChecksumSHA256 --output text > /dev/null
  echo "업로드: s3://$BUCKET/$1"
}

put "daily/$NAME"
if [[ "$(TZ=Asia/Seoul date +%u)" == 7 ]]; then   # %u: 월=1 … 일=7
  put "weekly/$NAME"
fi

# 로컬은 최근 LOCAL_KEEP 개만(업로드가 성공한 뒤에만 지운다 — 위에서 실패하면 set -e 로 여기까지 오지 않는다)
ls -1t "$BACKUP_DIR"/nook-*.dump | tail -n +"$((LOCAL_KEEP + 1))" | xargs -r rm -f
echo "완료: 로컬 $(ls -1 "$BACKUP_DIR"/nook-*.dump | wc -l)개 보관"
