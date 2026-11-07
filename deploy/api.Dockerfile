# syntax=docker/dockerfile:1
# Nook.Api 이미지 + 마이그레이션 1회성 이미지(target: migrate). DEP 2단계, Claude 초안.
# 빌드(리포 루트에서): docker build -f deploy/api.Dockerfile -t nook-api .
#                      docker build -f deploy/api.Dockerfile --target migrate -t nook-migrate .

ARG DOTNET_VERSION=10.0

# ---- 1. restore: csproj 만 먼저 복사 → 코드만 바뀌면 이 레이어(패키지 다운로드)는 캐시를 그대로 쓴다
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
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

# ---- 3. 마이그레이션 번들: EF 마이그레이션을 실행 파일 하나로. 운영 이미지에 SDK·dotnet-ef 를 넣지 않으려고 따로 만든다
FROM restore AS bundle
ARG TARGETARCH
COPY .editorconfig ./
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

# ---- target: migrate — 연결 문자열은 실행 시 환경변수 ConnectionStrings__Nook 로만 받는다(이미지에 없음)
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION} AS migrate
WORKDIR /migrate
COPY --from=bundle /out/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---- target: api (기본 — 마지막 스테이지)
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS api
WORKDIR /app
COPY --from=publish /app/publish ./
# 이미지 저장소(LocalDisk) 볼륨 자리. 이름 있는 볼륨은 처음 붙을 때 이 폴더의 소유자를 물려받는다 → 미리 app 소유로 만들지 않으면 비루트가 못 쓴다
RUN mkdir -p /app/data/images && chown -R $APP_UID /app/data
# .NET 8+ 공식 이미지에 있는 비루트 사용자(app, UID 1654). 포트 8080 은 비루트로 열 수 있다(1024 이상)
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Nook.Api.dll"]
