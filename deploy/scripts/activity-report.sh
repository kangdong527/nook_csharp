#!/usr/bin/env bash
# 사용자 활동 집계 — DEP 10단계 C-3, Claude 작성(2026-11-13). 결정(사용자): 이미 DB에 있는 데이터의 집계 숫자만.
#
# 사용(EC2, 릴리스 폴더 = 리포 루트): sudo deploy/scripts/activity-report.sh [일수, 기본 14]
# 출력: KST 날짜별 건수 + 전체 누계. 개인을 식별하는 값(이메일·닉네임·ID)은 출력하지 않는다.
#   개별 활동이 필요하면(문의·사고 조사) 운영자가 psql 로 직접 조회한다 — 그 접속은 Session Manager·CloudTrail 에 남는다.
#
# 읽는 법·한계:
#   - 가입 = users.id(UUIDv7)의 앞 48비트 = 생성 시각(ms). users 에 created_at 이 없다.
#     privacy_consented_at 은 쓰지 않는다 — 동의 기능(PR #40) 전 가입자는 NULL, 처리방침 재동의 시 바뀔 수 있다.
#     PG17 uuid_extract_timestamp() 는 v7 에 NULL(18부터 지원) → 직접 꺼낸다. 로컬 실측: 동의 시각과 4ms 차이
#   - 로그인 = 매직 링크를 실제로 쓴 수(consumed_at). "명"은 같은 날 같은 이메일을 1로 센다
#   - 탈퇴하면 그 회원의 행(가입·로그인 기록)이 지워져서 과거 날짜 숫자도 줄어든다 = "지금 남아 있는 것" 기준
#     (리뷰·좋아요는 탈퇴해도 작성자만 NULL 로 남아 그대로 센다)
#   - 접속(방문·화면 열람) 수는 어디에도 저장하지 않아서 없다
set -euo pipefail

DAYS="${1:-14}"
[[ "$DAYS" =~ ^[0-9]+$ ]] && (( DAYS >= 1 && DAYS <= 366 )) || { echo "일수는 1~366: $DAYS" >&2; exit 1; }
DB_CONTAINER="${DB_CONTAINER:-nook-deploy-db-1}"

# 날짜 경계는 KST 00시(리뷰 하루 기준 ReviewDay 와 같다). generate_series 로 활동이 0인 날도 행을 만든다.
SQL="
WITH d AS (
  SELECT g::date AS day
  FROM generate_series((now() AT TIME ZONE 'Asia/Seoul')::date - ($DAYS - 1),
                       (now() AT TIME ZONE 'Asia/Seoul')::date, interval '1 day') g
),
kst AS (SELECT 'Asia/Seoul'::text AS tz)
SELECT d.day AS \"날짜(KST)\",
  (SELECT count(*) FROM users u, kst
     WHERE (to_timestamp(('x' || left(replace(u.id::text, '-', ''), 12))::bit(48)::bigint / 1000.0)
            AT TIME ZONE kst.tz)::date = d.day) AS 가입,
  (SELECT count(DISTINCT lower(t.email)) FROM magic_link_tokens t, kst
     WHERE t.consumed_at IS NOT NULL AND (t.consumed_at AT TIME ZONE kst.tz)::date = d.day) AS \"로그인(명)\",
  (SELECT count(*) FROM reviews r, kst WHERE (r.created_at AT TIME ZONE kst.tz)::date = d.day) AS 리뷰,
  (SELECT count(*) FROM likes l, kst WHERE (l.created_at AT TIME ZONE kst.tz)::date = d.day) AS 좋아요,
  (SELECT count(*) FROM bookmarks b, kst WHERE (b.created_at AT TIME ZONE kst.tz)::date = d.day) AS 북마크,
  (SELECT count(*) FROM groups g, kst WHERE (g.created_at AT TIME ZONE kst.tz)::date = d.day) AS 그룹생성,
  (SELECT count(*) FROM group_members m, kst WHERE (m.joined_at AT TIME ZONE kst.tz)::date = d.day) AS 그룹합류
FROM d ORDER BY d.day;

SELECT (SELECT count(*) FROM users) AS 회원,
       (SELECT count(*) FROM places) AS 장소,
       (SELECT count(*) FROM reviews) AS 리뷰,
       (SELECT count(*) FROM likes) AS 좋아요,
       (SELECT count(*) FROM bookmarks) AS 북마크,
       (SELECT count(*) FROM groups) AS 그룹;
"

# TZ=Asia/Seoul 대신 UTC+9 — 시간대 데이터가 없는 셸(Git Bash)에서도 맞게. 한국은 서머타임이 없다
echo "== 최근 ${DAYS}일 활동 (KST) — 생성 $(date -u -d '+9 hours' '+%Y-%m-%d %H:%M') KST"
# 표준 입력으로 SQL 을 넘긴다(인자 따옴표 지옥 회피). 비밀번호 불필요 — 컨테이너 안 로컬 소켓
docker exec -i "$DB_CONTAINER" sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -X -q -P footer=off' <<< "$SQL"
