using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nook.Api.Http;
using Nook.Infrastructure;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// DEP 7단계 /health/live·/health/ready. 나눈 이유가 테스트의 핵심: DB가 죽으면 ready만 503, live는 200.
public sealed class HealthApiTests : IntegrationTestBase
{
    private readonly string _connectionString;

    public HealthApiTests(PostgresFixture fixture) : base(fixture) => _connectionString = fixture.ConnectionString;

    // 테스트 목록
    //
    // - Live_And_Ready_WithDatabase_Return200Healthy   (토큰 없이 — Fallback 정책 예외)
    // - Ready_WhenDatabaseUnreachable_Returns503_LiveStays200
    // - Ready_WhenUnhealthy_DoesNotLeakDetails         (예외 메시지·호스트를 응답에 넣지 않는다)
    // - Ready_WhenDatabaseUnreachable_KeepsCauseForLogs (응답엔 없지만 로그로 갈 결과엔 원인 예외가 있다 — 운영 롤백 실측에서 '(null)'만 남았던 것)
    // - Health_IsNotRateLimited                        (배포 스크립트 반복 확인이 전체 한도를 먹지 않게)

    // 닫힌 포트 — 연결이 즉시 거절된다. Timeout은 혹시 모를 대기를 짧게.
    private const string UnreachableDb = "Host=127.0.0.1;Port=1;Database=nook;Username=nook;Password=nook;Timeout=2";

    private static async Task<(HttpStatusCode Status, string Body)> Get(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Live_And_Ready_WithDatabase_Return200Healthy(string path)
    {
        using var factory = new NookApiFactory(_connectionString);

        var (status, body) = await Get(factory.CreateClient(), path);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Ready_WhenDatabaseUnreachable_Returns503_LiveStays200()
    {
        using var factory = new NookApiFactory(UnreachableDb);
        var client = factory.CreateClient();

        var ready = await Get(client, "/health/ready");
        var live = await Get(client, "/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.Status);
        Assert.Equal(HttpStatusCode.OK, live.Status);
    }

    [Fact]
    public async Task Ready_WhenUnhealthy_DoesNotLeakDetails()
    {
        using var factory = new NookApiFactory(UnreachableDb);

        var (_, body) = await Get(factory.CreateClient(), "/health/ready");

        Assert.Equal("Unhealthy", body);
    }

    // HealthCheckService가 Unhealthy 항목을 로그로 남길 때 쓰는 것이 이 결과의 Exception이다.
    // CanConnectAsync(기본)는 연결 예외를 삼키고 false만 돌려줘서 Exception이 null → 로그 message '(null)'.
    [Fact]
    public async Task Ready_WhenDatabaseUnreachable_KeepsCauseForLogs()
    {
        using var factory = new NookApiFactory(UnreachableDb);
        var health = factory.Services.GetRequiredService<HealthCheckService>();

        var report = await health.CheckHealthAsync(r => r.Tags.Contains(DependencyInjection.ReadyTag));

        var database = Assert.Single(report.Entries).Value;
        Assert.Equal(HealthStatus.Unhealthy, database.Status);
        Assert.NotNull(database.Exception);
    }

    [Fact]
    public async Task Health_IsNotRateLimited()
    {
        using var factory = new NookApiFactory(_connectionString, services =>
            services.Configure<RateLimitOptions>(o => o.GlobalPermitLimit = 2));
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await Get(client, "/health/live")).Status);
    }
}
