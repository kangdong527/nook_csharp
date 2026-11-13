#!/usr/bin/env bash
# OS 보안 패치 자동 적용 설치(1회, 설정을 바꿨을 때만 다시). DEP 10단계 Pillar, Claude 작성(2026-11-13).
# 결정(사용자): ① dnf-automatic + 최신 릴리스 추적. 기각: SSM Patch Manager(자동 재부팅·AL2023 릴리스 고정과의 동작 미검증), 매월 수동(잊음)
#
# 사용(EC2, SSM 세션):
#   cd /opt/nook/releases/$(cat /opt/nook/state/current-tag) && sudo deploy/scripts/install-auto-patch.sh
#
# 왜 releasever=latest 인가: AL2023 은 저장소를 설치 당시 릴리스에 고정한다. 보안 수정은 주로 새 릴리스로 나와서
#   고정된 채로는 '밀린 보안 업데이트 0'이 나온다(2026-11-13 실측: 고정 0 · latest 12 = Important 8, Medium 4).
# 하지 않는 것: 재부팅. 커널·glibc 는 설치만 되고 재부팅 전까지 예전 것이 돈다 → `sudo dnf needs-restarting -r` 로 확인 후 수동.
# 대가: docker·containerd 가 업데이트되면 데몬 재시작으로 컨테이너가 잠깐 끊긴다(restart: unless-stopped 로 복귀).
set -euo pipefail

[[ $EUID -eq 0 ]] || { echo "root 로 실행하세요(sudo)" >&2; exit 1; }
grep -q 'Amazon Linux release 2023' /etc/amazon-linux-release || { echo "AL2023 전용" >&2; exit 1; }

CONF=/etc/dnf/automatic.conf

# 1) 최신 릴리스 추적 — dnf 변수 파일. dnf·dnf-automatic 모두 읽는다
echo latest > /etc/dnf/vars/releasever

# 2) 설치 + 설정: 보안 업데이트만, 내려받고 실제로 적용까지
dnf -y -q install dnf-automatic
sed -i -E 's/^upgrade_type[[:space:]]*=.*/upgrade_type = security/; s/^apply_updates[[:space:]]*=.*/apply_updates = yes/' "$CONF"
# sed 는 줄이 없으면 조용히 넘어간다 → 결과를 확인한다
grep -qx 'upgrade_type = security' "$CONF" && grep -qx 'apply_updates = yes' "$CONF" \
  || { echo "$CONF 수정 실패 — [commands] 의 upgrade_type·apply_updates 를 직접 확인" >&2; exit 1; }

# 3) 실행 시각(06:30 KST) drop-in 후 타이머 켜기
install -d /etc/systemd/system/dnf-automatic.timer.d
install -m 644 deploy/systemd/dnf-automatic.timer.d/nook.conf /etc/systemd/system/dnf-automatic.timer.d/
systemctl daemon-reload
systemctl enable --now dnf-automatic.timer

echo "releasever: $(cat /etc/dnf/vars/releasever)"
grep -E '^(upgrade_type|apply_updates)' "$CONF"
systemctl list-timers dnf-automatic.timer --no-pager
