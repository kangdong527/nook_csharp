#!/usr/bin/env bash
# 백업 타이머 설치(1회, 유닛 파일을 바꿨을 때만 다시). DEP 9단계, Claude 작성(2026-11-11).
#
# 사용(EC2, SSM 세션):
#   cd /opt/nook/releases/$(cat /opt/nook/state/current-tag) && sudo deploy/scripts/install-backup-timer.sh
# 스크립트 본문(db-backup-s3.sh)은 서비스가 매번 현재 릴리스에서 찾는다 → 스크립트만 바꾼 배포는 다시 설치할 필요 없다.
set -euo pipefail

[[ $EUID -eq 0 ]] || { echo "root 로 실행하세요(sudo)" >&2; exit 1; }

install -m 644 deploy/systemd/nook-backup.service deploy/systemd/nook-backup.timer /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now nook-backup.timer
systemctl list-timers nook-backup.timer --no-pager
