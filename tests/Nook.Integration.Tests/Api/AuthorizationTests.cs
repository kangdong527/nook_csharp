using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nook.Api.Auth;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// Fallback 정책(인가 표시 없는 엔드포인트 = 인증 필요)과 JwtBearer 검증을 HTTP로 확인한다.
// 보호 엔드포인트 예시는 아직 /weatherforecast뿐이다. PL-01이 생기면 그쪽으로 바꾼다.
public sealed class AuthorizationTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    // - Protected_WithoutToken_Returns401
    // - Protected_WithIssuedToken_Returns200
    // - Protected_WithTokenSignedByOtherKey_Returns401     (서명 위조)
    // - Protected_WithExpiredToken_Returns401              (만료 + ClockSkew 30초 밖)
    // - Protected_WithExpiredToken_DoesNotRevealReason     (운영: WWW-Authenticate에 실패 사유 없음)
    // - UnknownRoute_WithoutToken_Returns401               (엔드포인트가 없어도 Fallback이 먼저)

    private const string ProtectedPath = "/weatherforecast";

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Protected_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_WithIssuedToken_Returns200()
    {
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(Guid.NewGuid());

        var response = await GetWithBearerAsync(ProtectedPath, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_WithTokenSignedByOtherKey_Returns401()
    {
        var jwt = _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var forger = new AccessTokenIssuer(Options.Create(new JwtOptions
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            SigningKey = "attacker-guessed-signing-key-0123456789abcdef",
        }), TimeProvider.System);

        var response = await GetWithBearerAsync(ProtectedPath, forger.Issue(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_WithExpiredToken_Returns401()
    {
        var jwt = _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        // 만료 시각이 1분 전 → ClockSkew(30초)로도 구제되지 않는다.
        var issuedAt = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(jwt.AccessTokenMinutes + 1);
        var issuer = new AccessTokenIssuer(Options.Create(jwt), new FixedClock(issuedAt));

        var response = await GetWithBearerAsync(ProtectedPath, issuer.Issue(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 개발 밖(IncludeErrorDetails=false)에서는 실패 사유를 응답 헤더에 담지 않는다(D5 이월 → DEP 7단계 확인).
    // 사유가 보이면 공격자가 서명 위조·만료·발급자 불일치를 구분해 시도를 좁힌다. 테스트 환경은 "Testing"이라 운영과 같은 쪽이다.
    [Fact]
    public async Task Protected_WithExpiredToken_DoesNotRevealReason()
    {
        var jwt = _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var issuer = new AccessTokenIssuer(Options.Create(jwt),
            new FixedClock(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(jwt.AccessTokenMinutes + 1)));

        var response = await GetWithBearerAsync(ProtectedPath, issuer.Issue(Guid.NewGuid()));

        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Null(challenge.Parameter);   // error="invalid_token", error_description="The token expired at ..." 가 없어야 한다
    }

    [Fact]
    public async Task UnknownRoute_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/no-such-route");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> GetWithBearerAsync(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
