using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Controllers;
using Nook.Api.Http;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// DEP 7단계: IP 요청 제한 + X-Forwarded-For 신뢰 경계. 수치는 운영 기본값(RateLimitOptions) 그대로 — 전체 한도 테스트만 그 안에서 낮춘다.
// 전체 한도와 인증 한도는 둘 다 통과해야 한다: 전체를 3으로 낮춘 채 매직 링크 5회를 보내면 4번째가 전체 한도로 429(첫 작성 때 실측).
// TestServer는 접속 IP가 없다 → PeerHeader로 받은 값을 RemoteIpAddress에 넣는다(ForwardedHeaders보다 앞, 실제 TCP 상대 역할).
public sealed class RateLimitApiTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // - MagicLink_SixthRequestFromSameClient_Returns429ProblemWithRetryAfter
    // - MagicLink_DifferentClientsBehindTrustedProxy_HaveSeparateLimits   (XFF를 믿고 클라이언트별로 센다)
    // - MagicLink_ForwardedForFromUntrustedPeer_IsIgnored                (위조 XFF로 칸을 바꿔 우회 못 함)
    // - Consume_IsCountedSeparatelyFromRequest                           (칸 = IP + 경로)
    // - GlobalLimit_AppliesToAnonymousEndpoints

    private const string PeerHeader = "X-Test-Peer";
    private const string TrustedProxy = "10.89.0.5";       // NookApiFactory.TrustedProxyNetwork 안 = web(BFF)
    private const string UntrustedPeer = "203.0.113.9";    // 신뢰 대역 밖에서 바로 붙은 상대
    private const int AuthLimit = 5;                       // RateLimitOptions 기본값

    private readonly string _connectionString;
    private NookApiFactory _factory;
    private HttpClient _client;

    public RateLimitApiTests(PostgresFixture fixture) : base(fixture)
    {
        _connectionString = fixture.ConnectionString;
        (_factory, _client) = CreateFactory();
    }

    public void Dispose() => _factory.Dispose();

    // tweak: 이 테스트에서만 수치를 바꿀 때. Configure는 BindConfiguration 뒤에 실행되어 이긴다.
    private (NookApiFactory, HttpClient) CreateFactory(Action<RateLimitOptions>? tweak = null)
    {
        var factory = new NookApiFactory(_connectionString, services =>
        {
            services.AddSingleton<IStartupFilter, PeerFromHeaderStartupFilter>();
            if (tweak is not null)
                services.Configure(tweak);
        });
        return (factory, factory.CreateClient());
    }

    private Task<HttpResponseMessage> RequestLink(string peer, string? forwardedFor = null) =>
        Send(HttpMethod.Post, "/auth/magic-link", peer, forwardedFor,
            JsonContent.Create(new MagicLinkRequest($"{Guid.NewGuid():N}@nook.test")));   // 이메일 한도(3/15분)와 섞이지 않게 매번 새 주소

    private Task<HttpResponseMessage> Send(HttpMethod method, string path, string peer, string? forwardedFor, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add(PeerHeader, peer);
        if (forwardedFor is not null)
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task MagicLink_SixthRequestFromSameClient_Returns429ProblemWithRetryAfter()
    {
        for (var i = 0; i < AuthLimit; i++)
            Assert.Equal(HttpStatusCode.Accepted, (await RequestLink(TrustedProxy, "198.51.100.1")).StatusCode);

        var response = await RequestLink(TrustedProxy, "198.51.100.1");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("RateLimit.Exceeded", problem!.Title);
        Assert.Equal(AuthLimit, _factory.Mail.Sent.Count);   // 거절된 요청은 메일을 만들지 않는다
    }

    [Fact]
    public async Task MagicLink_DifferentClientsBehindTrustedProxy_HaveSeparateLimits()
    {
        for (var i = 0; i < AuthLimit; i++)
            await RequestLink(TrustedProxy, "198.51.100.1");

        var otherClient = await RequestLink(TrustedProxy, "198.51.100.2");

        Assert.Equal(HttpStatusCode.Accepted, otherClient.StatusCode);
    }

    [Fact]
    public async Task MagicLink_ForwardedForFromUntrustedPeer_IsIgnored()
    {
        // 매번 다른 XFF를 적어도 신뢰 대역 밖이면 무시 → 모두 UntrustedPeer 한 칸으로 센다
        for (var i = 0; i < AuthLimit; i++)
            Assert.Equal(HttpStatusCode.Accepted, (await RequestLink(UntrustedPeer, $"198.51.100.{i + 10}")).StatusCode);

        var response = await RequestLink(UntrustedPeer, "198.51.100.99");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Consume_IsCountedSeparatelyFromRequest()
    {
        for (var i = 0; i < AuthLimit; i++)
            await RequestLink(TrustedProxy, "198.51.100.1");

        var consume = await Send(HttpMethod.Post, "/auth/magic-link/consume", TrustedProxy, "198.51.100.1",
            JsonContent.Create(new ConsumeMagicLinkRequest("unknown-token", Nickname: null)));

        Assert.Equal(HttpStatusCode.BadRequest, consume.StatusCode);   // 429가 아니라 업무 규칙의 응답
    }

    [Fact]
    public async Task GlobalLimit_AppliesToAnonymousEndpoints()
    {
        _factory.Dispose();
        (_factory, _client) = CreateFactory(o => o.GlobalPermitLimit = 3);   // 기본 300번을 보내지 않으려고 이 테스트만 낮춘다

        const string path = "/images/reviews/2026/11/09/0123456789abcdef0123456789abcdef.webp";   // 없는 이미지 → 404
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, path, TrustedProxy, "198.51.100.1")).StatusCode);

        var response = await Send(HttpMethod.Get, path, TrustedProxy, "198.51.100.1");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    // 파이프라인 맨 앞(Program.cs의 미들웨어보다 먼저)에서 접속 IP를 흉내 낸다.
    private sealed class PeerFromHeaderStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((http, nextMiddleware) =>
            {
                if (http.Request.Headers.TryGetValue(PeerHeader, out var peer))
                    http.Connection.RemoteIpAddress = IPAddress.Parse(peer!);
                return nextMiddleware(http);
            });
            next(app);
        };
    }
}
