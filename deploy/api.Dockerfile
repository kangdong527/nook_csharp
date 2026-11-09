# syntax=docker/dockerfile:1
# Nook.Api 이미지 + 마이그레이션 1회성 이미지(target: migrate). DEP 2단계, Claude 초안.
# 빌드(리포 루트에서): docker build -f deploy/api.Dockerfile -t nook-api .
#                      docker build -f deploy/api.Dockerfile --target migrate -t nook-migrate .

ARG DOTNET_VERSION=10.0

# ---- 1. restore: csproj 만 먼저 복사 → 코드만 바뀌면 이 레이어(패키지 다운로드)는 캐시를 그대로 쓴다
# --platform=$BUILDPLATFORM: SDK 는 빌드 머신 아키텍처로 돈다(QEMU 에뮬레이션 없음, DEP 8단계 — 운영 t4g = arm64).
#   publish 결과(UseAppHost=false 프레임워크 의존 DLL)는 아키텍처와 무관하고, 번들은 TARGETARCH 로 RID 를 골라 교차 빌드한다.
#   실행 이미지(aspnet·runtime-deps)만 대상 아키텍처 것을 받는다 — 아래 FROM 에 --platform 이 없는 이유.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src
COPY Directory.Build.props dotnet-tools.json ./
COPY src/Nook.SharedKernel/Nook.SharedKernel.csproj       src/Nook.SharedKernel/
COPY src/Nook.Domain/Nook.Domain.csproj                   src/Nook.Domain/
COPY src/Nook.Algorithms/Nook.Algorithms.csproj           src/Nook.Algorithms/
COPY src/Nook.Application/Nook.Application.csproj         src/Nook.Application/
COPY src/Nook.Infrastructure/Nook.Infrastructure.csproj   src/Nook.Infrastructure/
COPY src/Nook.Api/Nook.Api.csproj                         src/Nook.Api/
RUN dotnet restore src/Nook.Api/Nook.Api.csproj

# ---- 2. build + publish (프레임워크 의존 — 런타임은 aspnet 베이스 이미지에 있다)
FROM restore AS publish
COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Nook.Api/Nook.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false
# 이미지 저장소(LocalDisk) 볼륨 자리. 실행 이미지(chiseled)엔 셸이 없어 RUN mkdir 를 못 하므로 여기서 만들어 소유자째 복사한다
RUN mkdir -p /app/data/images

# ---- 3. 마이그레이션 번들: EF 마이그레이션을 실행 파일 하나로. 운영 이미지에 SDK·dotnet-ef 를 넣지 않으려고 따로 만든다
# 이 단계만 대상 플랫폼(arm64 면 QEMU)에서 돈다 — restore 를 잇지 않는다. 번들 도구가 시작 프로젝트(Nook.Api)를 --runtime 으로 빌드한 뒤
# 그 결과를 직접 로드해 DbContext 를 얻는데, 빌드 머신(x86)에서 linux-arm64 빌드 결과를 로드하면
# "Could not load file or assembly 'Nook.Api'"로 실패한다(2026-11-09 실측). api 의 publish 는 로드하지 않아 교차 빌드가 된다.
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS bundle
ARG TARGETARCH
WORKDIR /src
COPY Directory.Build.props dotnet-tools.json .editorconfig ./
COPY src/ src/
RUN dotnet tool restore
# TARGETARCH(amd64|arm64, buildx 가 채움) → .NET RID. t4g(ARM) 배포 때 arm64 로 빌드된다(8단계)
# self-contained 는 RID 별 런타임 팩이 필요하다 → 1단계(RID 없는 restore)와 별개로 RID 를 주고 다시 restore(NETSDK1047)
# 번들을 만들 때도 EF 가 Program.cs 를 Build 직전까지 돌려 DbContext 를 얻는다 → 연결 문자열·구현체 설정이 있어야 한다.
# 이 단계는 DB 에 접속하지 않으므로 이 RUN 에만 자리표시 값(비밀 아님)을 준다. 실제 값은 실행 시 환경변수로.
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-arm64 || echo linux-x64) \
 && dotnet restore src/Nook.Api/Nook.Api.csproj --runtime "$RID" \
 && ConnectionStrings__Nook="Host=build-placeholder" Email__Provider=Console ImageStorage__Provider=LocalDisk \
    dotnet ef migrations bundle \
      --project src/Nook.Infrastructure --startup-project src/Nook.Api \
      --configuration Release --runtime "$RID" --self-contained \
      --output /out/efbundle --verbose

# ---- 실행 이미지 = chiseled(-extra): 셸·apt·패키지 관리자 없음 → 공격면이 작고 OS 취약점이 거의 없다(DEP 3단계 Trivy).
#      기본 사용자가 이미 비루트 app(UID 1654). -extra = ICU·tzdata 포함. extra 가 아니면 세계화 불변 모드(InvariantGlobalization)로 돌아
#      문화권별 문자열 비교·서식이 개발 환경과 달라진다 → 같게 유지하려고 extra. (ReviewDay 는 고정 +09:00 이라 tzdata 와 무관)
#      대가: 컨테이너 안에서 sh·ls·find 를 못 쓴다. 파일 확인은 docker compose cp 나 볼륨으로.

# ---- target: migrate — 연결 문자열은 실행 시 환경변수 ConnectionStrings__Nook 로만 받는다(이미지에 없음)
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION}-noble-chiseled-extra AS migrate
WORKDIR /migrate
COPY --from=bundle /out/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---- target: api (기본 — 마지막 스테이지)
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra AS api
WORKDIR /app
COPY --from=publish /app/publish ./
# 이름 있는 볼륨은 처음 붙을 때 이 폴더의 소유자를 물려받는다 → app(1654) 소유로 복사해야 비루트가 쓸 수 있다
COPY --from=publish --chown=1654:1654 /app/data ./data
# 비루트 사용자(app, UID 1654). 포트 8080 은 비루트로 열 수 있다(1024 이상)
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Nook.Api.dll"]
