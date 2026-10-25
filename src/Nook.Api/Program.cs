using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nook.Api.Auth;
using Nook.Application.Auth;
using Nook.Application.Bookmarks;
using Nook.Application.Groups;
using Nook.Application.Likes;
using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 연결 문자열: 개발은 user-secrets, 배포는 환경변수 ConnectionStrings__Nook
var connectionString = builder.Configuration.GetConnectionString("Nook")
    ?? throw new InvalidOperationException("연결 문자열 'Nook'이 설정되지 않았습니다.");
builder.Services.AddInfrastructure(connectionString);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddConsoleEmailTransport();
    builder.Services.AddInMemoryReviewImageStorage();
}

builder.Services.AddSingleton(TimeProvider.System);

// 유스케이스(Application). 저장소가 Scoped라 같은 수명으로 둔다.
builder.Services.AddScoped<RequestMagicLinkHandler>();
builder.Services.AddScoped<ConsumeMagicLinkHandler>();
builder.Services.AddScoped<ConfirmPlaceHandler>();
builder.Services.AddScoped<GetPlaceHandler>();
builder.Services.AddScoped<GetPlaceReviewsHandler>();
builder.Services.AddScoped<SaveBookmarkHandler>();
builder.Services.AddScoped<RemoveBookmarkHandler>();
builder.Services.AddScoped<GetMyPlacesHandler>();
builder.Services.AddScoped<AddLikeHandler>();
builder.Services.AddScoped<RemoveLikeHandler>();
builder.Services.AddScoped<CreateReviewHandler>();
builder.Services.AddScoped<EditReviewHandler>();
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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 개발 환경의 API 문서는 공개(Fallback 정책 제외)
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
