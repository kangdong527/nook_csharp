using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Nook.Api.Controllers;
using Nook.Application.Auth;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// AU-01·AU-02를 HTTP로. 업무 규칙(속도 제한·만료·롤백)은 핸들러 테스트가 이미 본다.
// 여기서 보는 것은 Api 계층의 몫: 라우트·[AllowAnonymous]·요청 바인딩·ErrorType → 상태 코드·응답 JSON 모양.
public sealed class AuthApiTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // AU-01 POST /auth/magic-link
    // - RequestMagicLink_ValidEmail_Returns202AndQueuesMail
    // - RequestMagicLink_InvalidEmail_Returns400Problem          (null·빈 값·형식 오류 — 모두 같은 코드)
    //
    // AU-02 POST /auth/magic-link/consume
    // - Consume_NewEmailWithoutNickname_Returns200NeedsNickname
    // - Consume_NewEmailWithNickname_ReturnsTokenThatPassesAuth  (메일 링크 → 가입 → 받은 토큰으로 보호 API)
    // - Consume_WhenNicknameTaken_Returns409Problem
    // - Consume_WhenTokenUnknown_Returns400Problem

    private const string NewEmail = "new@nook.test";

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    // ── AU-01 ─────────────────────────────────────────────

    [Fact]
    public async Task RequestMagicLink_ValidEmail_Returns202AndQueuesMail()
    {
        var response = await _client.PostAsJsonAsync("/auth/magic-link", new MagicLinkRequest(NewEmail));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var mail = Assert.Single(_factory.Mail.Sent);
        Assert.Equal(NewEmail, mail.To);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task RequestMagicLink_InvalidEmail_Returns400Problem(string? email)
    {
        var response = await _client.PostAsJsonAsync("/auth/magic-link", new MagicLinkRequest(email));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, UserErrors.EmailFormat.Code);
        Assert.Empty(_factory.Mail.Sent);
    }

    // ── AU-02 ─────────────────────────────────────────────

    [Fact]
    public async Task Consume_NewEmailWithoutNickname_Returns200NeedsNickname()
    {
        var token = await RequestLinkAsync(NewEmail);

        var body = await ConsumeOkAsync(token, nickname: null);

        Assert.True(body.NeedsNickname);
        Assert.Equal(NewEmail, body.Email);   // 가입 폼 표시용 — 링크의 이메일
        Assert.Null(body.AccessToken);
    }

    [Fact]
    public async Task Consume_NewEmailWithNickname_ReturnsTokenThatPassesAuth()
    {
        var token = await RequestLinkAsync(NewEmail);

        var body = await ConsumeOkAsync(token, "누크");

        Assert.False(body.NeedsNickname);
        Assert.NotNull(body.AccessToken);
        var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Consume_WhenNicknameTaken_Returns409Problem()
    {
        await ConsumeOkAsync(await RequestLinkAsync("old@nook.test"), "누크");
        var token = await RequestLinkAsync(NewEmail);

        var response = await PostConsumeAsync(token, "누크");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, UserErrors.NicknameTaken.Code);
    }

    [Fact]
    public async Task Consume_WhenTokenUnknown_Returns400Problem()
    {
        var (unknown, _) = MagicLinkSecret.Create();

        var response = await PostConsumeAsync(unknown, "누크");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, AuthErrors.InvalidLink.Code);
    }

    // ── 도우미 ────────────────────────────────────────────

    // 실제 흐름처럼 요청 API를 거쳐 메일(기록용 큐)에서 원문 토큰을 꺼낸다.
    private async Task<string> RequestLinkAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/auth/magic-link", new MagicLinkRequest(email));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return _factory.Mail.Sent.Last(m => m.To == email).RawToken;
    }

    private Task<HttpResponseMessage> PostConsumeAsync(string token, string? nickname) =>
        _client.PostAsJsonAsync("/auth/magic-link/consume", new ConsumeMagicLinkRequest(token, nickname, Gender: "none"));

    private async Task<ConsumeMagicLinkResponse> ConsumeOkAsync(string token, string? nickname)
    {
        var response = await PostConsumeAsync(token, nickname);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ConsumeMagicLinkResponse>())!;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(code, problem?.Title);
    }
}
