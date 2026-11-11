#!/usr/bin/env bash
# 복원 리허설 — 덤프를 버리는 Postgres 컨테이너에 복원하고, 테이블별 행 수를 지금 DB 와 대조한다. DEP 9단계, Claude 작성(2026-11-09).
#
# 사용(리포 루트): deploy/scripts/db-restore-check.sh <덤프 파일>
# 결과: 테이블별 원본/복원 행 수, 복원에 걸린 시간(RTO 의 DB 부분). 하나라도 다르면 종료 코드 1.
#
# "백업이 있다"와 "복원된다"는 다르다 — 덤프가 깨졌거나, 확장·역할이 빠졌거나, 버전이 안 맞으면 복원할 때야 드러난다.
# 원본 비교의 한계: 원본 행 수는 지금 센다 → 덤프 뒤 쓰기가 있으면 차이가 난다. 리허설은 쓰기가 없는 시간에(로컬은 해당 없음).
set -euo pipefail

DUMP="${1:?덤프 파일 경로를 주세요}"
[[ -s "$DUMP" ]] || { echo "덤프가 없거나 비었습니다: $DUMP" >&2; exit 1; }

# compose 대신 컨테이너 이름(2026-11-11) — db-backup.sh 와 같은 이유(EC2 에서 compose 보간 실패)
DB_CONTAINER="${DB_CONTAINER:-nook-deploy-db-1}"
SCRATCH="nook-restore-check-$$"
total_start=$(date +%s)   # RTO 의 서버 부분 = 빈 DB 기동 + 복원(덤프를 서버로 가져오는 시간은 밖에서 따로 잰다)
# 운영과 같은 메이저 버전 — 다르면 리허설이 운영 복원을 증명하지 못한다(compose.yaml db 이미지와 맞출 것)
IMAGE="postgres:17"

cleanup() { docker rm -f "$SCRATCH" > /dev/null 2>&1 || true; }
trap cleanup EXIT

# 네트워크 없이 띄운다 — 리허설 DB 는 아무와도 연결될 필요가 없다. 비밀번호는 이 컨테이너 안에서만 쓰는 임시 값.
docker run -d --name "$SCRATCH" --network none -e POSTGRES_PASSWORD=restore-check -e POSTGRES_DB=nook "$IMAGE" > /dev/null
for _ in $(seq 1 60); do
  docker exec "$SCRATCH" pg_isready -U postgres -d nook > /dev/null 2>&1 && break
  sleep 1
done
docker exec "$SCRATCH" pg_isready -U postgres -d nook > /dev/null

# --no-owner: 운영 역할(nook)이 리허설 DB 에 없어도 된다. --exit-on-error: 한 객체라도 실패하면 리허설 실패.
start=$(date +%s)
docker exec -i "$SCRATCH" pg_restore -U postgres -d nook --no-owner --exit-on-error < "$DUMP"
elapsed=$(( $(date +%s) - start ))

# public 스키마의 모든 테이블(마이그레이션 이력 포함)을 정확히 센다. 테이블 목록을 하드코딩하지 않는다.
COUNT_SQL="SELECT string_agg(format('SELECT %L AS t, count(*) AS n FROM %I', tablename, tablename), ' UNION ALL ' ORDER BY tablename) FROM pg_tables WHERE schemaname = 'public'"
count_rows() {  # $@ = psql 을 실행할 docker 명령 앞부분
  local q; q=$("$@" -Atc "$COUNT_SQL")
  "$@" -AtF '|' -c "$q ORDER BY t"
}

source_counts=$(count_rows docker exec "$DB_CONTAINER" sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" "$@"' psql)
restored_counts=$(count_rows docker exec "$SCRATCH" psql -U postgres -d nook)

printf '%-28s %10s %10s\n' table source restored
status=0
while IFS='|' read -r table n; do
  restored=$(grep -E "^${table}\|" <<< "$restored_counts" | cut -d'|' -f2 || true)
  mark=""; [[ "$n" == "$restored" ]] || { mark="  ← 다름"; status=1; }
  printf '%-28s %10s %10s%s\n' "$table" "$n" "${restored:-없음}" "$mark"
done <<< "$source_counts"

echo "복원 시간: ${elapsed}s · 기동 포함 전체: $(( $(date +%s) - total_start ))s (덤프 $(wc -c < "$DUMP") bytes)"
if (( status == 0 )); then echo "복원 리허설 통과"; else echo "복원 리허설 실패 — 행 수가 다릅니다" >&2; fi
exit $status
