# syntax=docker/dockerfile:1
# web(Next.js) 이미지. DEP 2단계, Claude 초안.
# 빌드(리포 루트에서): docker build -f deploy/web.Dockerfile --build-arg NEXT_PUBLIC_KAKAO_MAP_KEY=... -t nook-web .

ARG NODE_VERSION=24-alpine

# ---- 1. deps: lockfile 그대로(npm ci). package*.json 만 먼저 → 코드만 바뀌면 캐시
FROM node:${NODE_VERSION} AS deps
WORKDIR /app
COPY web/package.json web/package-lock.json ./
RUN npm ci

# ---- 2. build
FROM node:${NODE_VERSION} AS build
WORKDIR /app
# NEXT_PUBLIC_* 는 빌드 때 JS 번들에 문자열로 박힌다 → 실행 시 환경변수로는 바뀌지 않는다.
# 카카오 JS 키는 원래 브라우저에 공개되는 값(도메인 등록으로 보호)이라 build-arg 로 넘겨도 된다. 비밀값은 절대 build-arg 로 넘기지 않는다(이미지 히스토리에 남음)
ARG NEXT_PUBLIC_KAKAO_MAP_KEY
ENV NEXT_PUBLIC_KAKAO_MAP_KEY=$NEXT_PUBLIC_KAKAO_MAP_KEY
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY web/ ./
RUN npm run build

# ---- 3. runtime: standalone 출력(next.config.js output: "standalone")만 — node_modules 전체·소스 없음
FROM node:${NODE_VERSION} AS web
WORKDIR /app
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1 PORT=3300 HOSTNAME=0.0.0.0
# 실행엔 node 만 필요하다. 이미지에 딸려 오는 npm·corepack·yarn 은 지운다 — 그 자체의 의존성(undici·brace-expansion 등)이
# Trivy HIGH 로 잡히고, 침입자가 쓸 수 있는 도구도 줄인다(DEP 3단계)
RUN rm -rf /usr/local/lib/node_modules/npm /usr/local/lib/node_modules/corepack \
           /usr/local/bin/npm /usr/local/bin/npx /usr/local/bin/corepack /opt/yarn* /usr/local/bin/yarn /usr/local/bin/yarnpkg
# node 이미지에 있는 비루트 사용자(node, UID 1000). 파일 소유자도 node 로
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
COPY --from=build --chown=node:node /app/public ./public
USER node
EXPOSE 3300
CMD ["node", "server.js"]
