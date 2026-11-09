using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Nook.Api.Http;

// IP 기준 요청 제한(DEP 7단계, 2026-11-09 사용자 결정: 전체 + 인증 엄격, 시작값은 아래 기본값 — 거절 로그를 보고 조정).
// IP는 ForwardedHeaders가 바꾼 RemoteIpAddress(Caddy가 적고 BFF가 넘긴 값). 신뢰 대역 밖에서 온 XFF는 무시된다.
// 저장소는 프로세스 메모리 — 인스턴스가 하나라 충분하다(늘리면 분산 저장소 필요). 재시작하면 창이 초기화된다.
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    // 모든 요청. 지도 화면은 이미지 GET이 많고, 통신사 CGNAT·학교 공유 IP 뒤 여러 명을 한 IP로 본다.
    public int GlobalPermitLimit { get; set; } = 300;
    public TimeSpan GlobalWindow { get; set; } = TimeSpan.FromMinutes(1);

    // 매직 링크 요청·소비. 이메일당 제한(MagicLinkPolicy 3/15분)이 못 막는 "한 IP가 주소를 바꿔 가며" 메일 폭탄·SES 비용을 막는다.
    public int AuthPermitLimit { get; set; } = 5;
    public TimeSpan AuthWindow { get; set; } = TimeSpan.FromMinutes(10);
}

public static class RateLimiting
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddNookRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>().BindConfiguration(RateLimitOptions.SectionName);

        services.AddRateLimiter(limiter =>
        {
            // 전체 한도와 엔드포인트 한도는 둘 다 통과해야 한다(전체 먼저).
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                var options = OptionsOf(http);
                return RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.GlobalPermitLimit,
                    Window = options.GlobalWindow,
                    QueueLimit = 0,   // 기다리게 하지 않고 바로 거절 — 대기열은 공격자에게 서버 자원을 쥐여 준다
                });
            });

            // 칸 = IP + 경로: 요청(메일 비용)과 소비(토큰 대입)는 막는 대상이 달라 따로 센다.
            // 하나로 세면 가입 한 번(요청 1 + 소비 2 — 닉네임 필요 → 다시 소비)이 5회 중 3회를 쓴다.
            limiter.AddPolicy(AuthPolicy, http =>
            {
                var options = OptionsOf(http);
                return RateLimitPartition.GetFixedWindowLimiter($"{ClientKey(http)}|{http.Request.Path}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitLimit,
                    Window = options.AuthWindow,
                    QueueLimit = 0,
                });
            });

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

                // 본문의 이메일은 남기지 않는다(개인정보) — IP와 경로만.
                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Nook.RateLimit")
                    .LogWarning("요청 제한 초과: {ClientIp} {Method} {Path}", ClientKey(http), http.Request.Method, http.Request.Path);

                // 다른 오류와 같은 계약: title = 오류 코드(ErrorResults)
                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "RateLimit.Exceeded",
                        Detail = "요청이 너무 많습니다. Retry-After 뒤에 다시 시도하세요.",
                    },
                });
            };
        });

        return services;
    }

    // 설정은 요청마다 읽는다(값은 IOptions라 캐시됨) — 테스트가 DI로 수치를 바꿀 수 있게.
    private static RateLimitOptions OptionsOf(HttpContext http) =>
        http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

    // IP를 모르면(테스트 서버 등) 한 칸으로 묶는다 — 모르는 요청끼리 한도를 나눠 쓴다(열어 두는 쪽보다 안전).
    private static string ClientKey(HttpContext http) =>
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
