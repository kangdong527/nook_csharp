using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nook.Api.Auth;
using Nook.Api.Http;
using Nook.Application.Auth;
using Nook.Application.Bookmarks;
using Nook.Application.Groups;
using Nook.Application.Likes;
using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.Application.Users;
using Nook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 연결 문자열: 개발은 user-secrets, 배포는 환경변수 ConnectionStrings__Nook
var connectionString = builder.Configuration.GetConnectionString("Nook")
    ?? throw new InvalidOperationException("연결 문자열 'Nook'이 설정되지 않았습니다.");
builder.Services.AddInfrastructure(connectionString);

// 바깥 경계 구현체는 환경 이름이 아니라 설정값으로 고른다(DEP 2단계, 사용자 결정) — compose(Production)에서도 콘솔·로컬 디스크로 띄우고,
// 7단계에서 Ses·S3 를 값 하나로 추가한다. 개발만 기본값이 있고, 그 밖의 환경은 값이 없거나 모르는 값이면 시작에서 실패한다(fail-fast).
var devDefault = builder.Environment.IsDevelopment();
switch (builder.Configuration["Email:Provider"] ?? (devDefault ? "Console" : null))
{
    case "Console": builder.Services.AddConsoleEmailTransport(); break;
    // 운영(DEP 7단계). 보내는 주소·리전 필수 — 빠지면 시작에서 실패. 실제 발송은 SES 도메인 인증·운영 승인 뒤
    case "Ses":
        builder.Services.AddSesEmailTransport(
            builder.Configuration["Email:Ses:From"] ?? throw new InvalidOperationException("Email:Ses:From 설정이 없습니다(예: no-reply@도메인)."),
            builder.Configuration["Email:Ses:Region"] ?? throw new InvalidOperationException("Email:Ses:Region 설정이 없습니다."));
        break;
    case var other: throw new InvalidOperationException($"Email:Provider 설정이 없거나 지원하지 않는 값입니다: '{other}' (지원: Console, Ses)");
}
switch (builder.Configuration["ImageStorage:Provider"] ?? (devDefault ? "LocalDisk" : null))
{
    // 로컬 디스크(FE-2 이미지 A안) — 재시작해도 남는다. 컨테이너에서는 LocalRoot 를 볼륨 경로로 준다. 통합 테스트는 메모리 구현으로 덮어쓴다.
    case "LocalDisk":
        builder.Services.AddLocalDiskImageStorage(
            builder.Configuration["ImageStorage:LocalRoot"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "images"));
        break;
    // 운영(dep-plan 결정 3). 버킷·리전은 필수 — 빠지면 시작에서 실패. ServiceUrl은 S3 호환 서버로 시험할 때만.
    case "S3":
        builder.Services.AddS3ImageStorage(
            builder.Configuration["ImageStorage:S3:Bucket"] ?? throw new InvalidOperationException("ImageStorage:S3:Bucket 설정이 없습니다."),
            builder.Configuration["ImageStorage:S3:Region"] ?? throw new InvalidOperationException("ImageStorage:S3:Region 설정이 없습니다."),
            builder.Configuration["ImageStorage:S3:ServiceUrl"]);
        break;
    case var other: throw new InvalidOperationException($"ImageStorage:Provider 설정이 없거나 지원하지 않는 값입니다: '{other}' (지원: LocalDisk, S3)");
}

// 키 → URL 기준 주소. BFF 중계 경로(브라우저 → Next → API → 저장소). S3여도 그대로 — 버킷은 비공개, API가 프록시(2026-11-09 결정).
builder.Services.AddReviewImageUrls(builder.Configuration["ImageStorage:PublicBaseUrl"] ?? "/api/nook/images/");

builder.Services.AddSingleton(TimeProvider.System);

// 클라이언트 IP(7단계 — IP RateLimiter의 전제). 요청은 Caddy → web(BFF) → api라 api가 보는 상대는 web 컨테이너다.
// X-Forwarded-For는 KnownNetworks(web이 있는 backend 서브넷)에서 온 것만 믿고, 맨 오른쪽 한 칸(ForwardLimit 1 = BFF가 적은 값)만 쓴다.
// 개발 밖에서는 필수(fail-fast). 주의: 신뢰 목록이 비면 미들웨어는 "모두 믿음"으로 동작한다 → 비었으면 아예 켜지 않는다.
var knownNetworks = (builder.Configuration["ForwardedHeaders:KnownNetworks"] ?? (devDefault ? "" : null)
        ?? throw new InvalidOperationException("ForwardedHeaders:KnownNetworks 설정이 없습니다(예: 172.30.0.0/24, 쉼표로 여러 개)."))
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(System.Net.IPNetwork.Parse)
    .ToList();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;   // Proto·Host는 쓰지 않는다 — api는 내부 HTTP만
    options.ForwardLimit = 1;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    knownNetworks.ForEach(options.KnownIPNetworks.Add);
});
builder.Services.AddNookRateLimiting();
builder.Services.AddHealthChecks().AddDatabaseHealthCheck();

// 유스케이스(Application). 저장소가 Scoped라 같은 수명으로 둔다.
builder.Services.AddScoped<RequestMagicLinkHandler>();
builder.Services.AddScoped<ConsumeMagicLinkHandler>();
builder.Services.AddScoped<ConfirmPlaceHandler>();
builder.Services.AddScoped<GetPlaceHandler>();
builder.Services.AddScoped<GetPlaceReviewsHandler>();
builder.Services.AddScoped<SaveBookmarkHandler>();
builder.Services.AddScoped<RemoveBookmarkHandler>();
builder.Services.AddScoped<GetMyPlacesHandler>();
builder.Services.AddScoped<GetMeHandler>();
builder.Services.AddScoped<GetMyReviewsHandler>();
builder.Services.AddScoped<ChangeNicknameHandler>();
builder.Services.AddScoped<ChangeAvatarHandler>();
builder.Services.AddScoped<RemoveAvatarHandler>();
builder.Services.AddScoped<RenameGroupHandler>();
builder.Services.AddScoped<GetMyGroupsHandler>();
builder.Services.AddScoped<GetGroupPlacesHandler>();
builder.Services.AddScoped<WithdrawHandler>();
builder.Services.AddScoped<AddLikeHandler>();
builder.Services.AddScoped<RemoveLikeHandler>();
builder.Services.AddScoped<CreateReviewHandler>();
builder.Services.AddScoped<EditReviewHandler>();
builder.Services.AddScoped<GetReviewForEditHandler>();
builder.Services.AddScoped<DeleteReviewHandler>();
builder.Services.AddScoped<CreateGroupHandler>();
builder.Services.AddScoped<JoinGroupHandler>();
builder.Services.AddScoped<LeaveGroupHandler>();

// 인증: API가 직접 서명한 Access 토큰(HS256)만 받는다(D5 결정)
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<AccessTokenIssuer>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        // 기본값(true)이면 "sub"가 ClaimTypes.NameIdentifier(긴 URI)로 바뀌어 들어온다. 발급한 이름 그대로 읽기 위해 끈다.
        bearer.MapInboundClaims = false;
        // 기본값(true)이면 401 응답의 WWW-Authenticate에 "토큰이 몇 시에 만료됨" 같은 실패 사유가 담긴다. 개발에서만 보인다.
        bearer.IncludeErrorDetails = builder.Environment.IsDevelopment();
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            // 헤더의 alg를 믿지 않고 발급한 알고리즘만 허용한다.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            // 기본 5분은 1시간 토큰에 비해 크다. 서버가 하나라 시계 차이는 작다.
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
// 기본 잠금: 인가 표시([Authorize]·[AllowAnonymous])가 없는 엔드포인트는 인증된 사용자만 부른다.
// 공개할 엔드포인트(로그인 등)에만 [AllowAnonymous]를 붙인다. [Authorize]만 붙인 곳은 이 정책이 아니라 DefaultPolicy를 쓴다.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddControllers();
// 처리되지 않은 예외 → ProblemDetails. 회원 FK 위반(탈퇴 후 남은 토큰)만 401로 바꾸고 나머지는 500(PF-02).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnknownUserExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// 맨 앞: 뒤의 모든 미들웨어(예외 로그·RateLimiter)가 바뀐 RemoteIpAddress를 본다.
if (knownNetworks.Count > 0)
    app.UseForwardedHeaders();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 개발 환경의 API 문서는 공개(Fallback 정책 제외)
    app.MapOpenApi().AllowAnonymous();
}

// UseHttpsRedirection 없음(DEP 7단계): TLS는 Caddy에서 끝나고 api는 내부 HTTP로만 받는다 — 리다이렉트할 HTTPS 포트가 없어
// "Failed to determine the https port" 경고만 남겼다. HTTP→HTTPS 리다이렉트와 HSTS는 브라우저와 맞닿은 Caddy 몫.

// 인증 앞: 거절할 요청은 JWT 검증 비용도 쓰지 않는다. 엔드포인트 한도([EnableRateLimiting])는 라우팅 뒤라야 보인다 —
// WebApplication은 UseRouting을 파이프라인 맨 앞에 암묵적으로 넣는다.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 헬스체크(DEP 7단계, 2026-11-09 결정): live = 프로세스가 요청을 받는가(의존성 없음), ready = DB까지(8단계 배포 게이트·롤백 판단).
// 재시작 판단에 ready를 쓰지 않는다 — DB 순간 장애가 멀쩡한 api 재시작으로 번진다.
// 응답은 상태 문자열만(기본 writer) — 예외 메시지·연결 정보를 내보내지 않는다. api는 바깥에 경로가 없다(Caddy → web만).
// 익명(Fallback 정책 예외), 요청 제한 제외(전체 한도 포함) — 배포 스크립트의 반복 확인이 한도를 먹지 않게.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous().DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(DependencyInjection.ReadyTag) })
    .AllowAnonymous().DisableRateLimiting();

app.Run();
