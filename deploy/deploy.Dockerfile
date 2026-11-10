# 배포 파일 묶음 이미지 nook-deploy:<sha> — DEP 8단계 결정 D8-1. Claude 작성(2026-11-10).
# 실행하는 이미지가 아니다(scratch, 셸 없음). SSM 문서 nook-deploy 가 docker create + docker cp 로 /release 를 꺼내
# EC2 의 /opt/nook/releases/<sha>/ 에 둔다 → 앱 이미지와 같은 sha 로 compose·Caddyfile·prod.env·스크립트가 함께 움직인다.
# 비밀값은 넣지 않는다(prod.env 는 설정값만, 비밀값은 배포 때 SSM 에서 읽는다).
#   docker build -f deploy/deploy.Dockerfile -t nook-deploy .
FROM scratch
COPY deploy/compose.yaml deploy/compose.prod.yaml deploy/Caddyfile deploy/prod.env /release/deploy/
COPY deploy/scripts/ /release/deploy/scripts/
