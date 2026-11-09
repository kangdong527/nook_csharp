using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nook.Api.Controllers;
using Nook.Api.Http;
using Nook.Application.Auth;
using Nook.Infrastructure.Auth;
using Nook.Integration.Tests.Infrastructure;
using Xunit.Abstractions;

namespace Nook.Integration.Tests.Api;

// A1 측정(D5 결정의 측정 기준): 같은 이메일 동시 100요청 → 실제 발송 수 ≤ MagicLinkPolicy.MaxIssuesPerWindow.
// 기본 NookApiFactory 는 메일 큐를 기록용으로 바꿔 워커를 비워 둔다 → 여기서는 실제 큐·워커를 살리고 발송기만 기록용으로 바꾼다.
public sealed class MagicLinkMailCapApiTests(PostgresFixture fixture, ITestOutputHelper output) : IntegrationTestBase(fixture)
{
    private const string Target = "victim@nook.test";
    private const int Concurrent = 100;

    [Fact]
    public async Task ConcurrentRequestsForSameEmail_SendAtMostPolicyLimit()
    {
        using var factory = new NookApiFactory(Db.ConnectionString, services =>
        {
            // 실제 큐(워커가 읽는 같은 인스턴스)로 되돌린다
            services.RemoveAll<IMagicLinkMailQueue>();
            services.AddSingleton<IMagicLinkMailQueue>(sp => sp.GetRequiredService<ChannelMagicLinkMailQueue>());
            services.RemoveAll<IEmailTransport>();
            services.AddSingleton<RecordingTransport>();
            services.AddSingleton<IEmailTransport>(sp => sp.GetRequiredService<RecordingTransport>());
            // IP 제한(A3)은 이 측정의 대상이 아니다 — 여러 IP 에서 온 분산 요청을 흉내 낸다
            services.Configure<RateLimitOptions>(o => { o.AuthPermitLimit = 10_000; o.GlobalPermitLimit = 10_000; });
        });
        var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, Concurrent)
            .Select(_ => client.PostAsJsonAsync("/auth/magic-link", new MagicLinkRequest(Target))));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Accepted, r.StatusCode));   // 항상 202(D5)

        // 워커가 큐를 다 비울 때까지(발송기는 즉시 반환)
        var queue = factory.Services.GetRequiredService<ChannelMagicLinkMailQueue>();
        for (var i = 0; i < 100 && queue.Reader.Count > 0; i++)
            await Task.Delay(50);
        await Task.Delay(200);

        await using var db = Db.CreateContext();
        var issued = await db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM magic_link_tokens WHERE lower(email) = {Target}")
            .SingleAsync();
        var sent = factory.Services.GetRequiredService<RecordingTransport>().Count;
        output.WriteLine($"동시 {Concurrent}요청 → Q2 통과(토큰 저장) {issued}건 → 실제 발송 {sent}통 (상한 {MagicLinkPolicy.MaxIssuesPerWindow})");

        Assert.InRange(sent, 1, MagicLinkPolicy.MaxIssuesPerWindow);
        Assert.True(issued >= sent);
    }

    private sealed class RecordingTransport : IEmailTransport
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);

        public Task SendAsync(string to, string subject, string body, CancellationToken ct)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }
}
