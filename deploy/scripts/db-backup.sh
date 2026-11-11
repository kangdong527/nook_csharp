#!/usr/bin/env bash
# DB 백업 — compose 의 db 컨테이너에서 pg_dump(커스텀 형식) → 파일. DEP 9단계, Claude 작성(2026-11-09).
#
# 사용(리포 루트): deploy/scripts/db-backup.sh [출력 폴더, 기본 ./backups]
# 운영(9단계): 하루 1회(RPO 24시간) 이 스크립트 → S3 업로드. 보존은 매일 7개 + 주 4개(S3 수명 주기 규칙) — 2026-11-09 사용자 결정
#
# 왜 -Fc(커스텀 형식)인가: 압축되고, pg_restore 가 테이블 단위 선택 복원·목록 확인(--list)을 할 수 있다. 평문 SQL 은 둘 다 안 된다.
# 일관성: pg_dump 는 한 스냅샷(트랜잭션)으로 읽는다 → 서비스를 멈추지 않아도 덤프 안에서는 테이블끼리 FK 가 맞는다.
# 주의: 덤프에는 회원 이메일 등 개인정보가 들어 있다 → 파일 권한 600(umask 077), 리포에 넣지 않는다(.gitignore backups/).
set -euo pipefail
umask 077

# compose 대신 컨테이너 이름으로 직접 exec(2026-11-11) — compose 는 파일을 읽을 때 ${POSTGRES_PASSWORD:?} 보간이 필요해서
# EC2(.env 가 /opt/nook/.env)에서는 --env-file 없이 실패한다. 프로젝트 이름이 nook-deploy 로 고정이라 로컬·운영 이름이 같다.
DB_CONTAINER="${DB_CONTAINER:-nook-deploy-db-1}"
OUT_DIR="${1:-./backups}"
mkdir -p "$OUT_DIR"
FILE="${BACKUP_FILE:-$OUT_DIR/nook-$(date -u +%Y%m%dT%H%M%SZ).dump}"   # BACKUP_FILE: 호출자(db-backup-s3.sh)가 이름을 정할 때

# 인자에 경로를 넘기지 않고 표준 출력으로 받는다 — Git Bash 의 경로 변환(/tmp → C:/...)을 피하고, 컨테이너에 파일을 남기지 않는다.
# 사용자·DB 이름은 컨테이너 환경변수에서 — 비밀번호는 컨테이너 안 로컬 소켓(trust)이라 필요 없다.
docker exec "$DB_CONTAINER" sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "$FILE"

# 무결성 점검: 비었거나 형식이 깨졌으면 pg_restore --list 가 실패한다(덤프가 중간에 끊긴 경우 포함). -i: 표준 입력을 넘긴다
if [[ ! -s "$FILE" ]] || ! docker exec -i "$DB_CONTAINER" pg_restore --list < "$FILE" > /dev/null; then
  echo "백업 실패: 덤프가 비었거나 읽을 수 없습니다 — $FILE" >&2
  exit 1
fi

echo "백업 완료: $FILE ($(wc -c < "$FILE") bytes)"
