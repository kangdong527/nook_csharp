using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Application.Auth;
using Nook.Domain.Users;
using Nook.Infrastructure;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;

namespace Nook.Integration.Tests.Auth;

// AU-02 매직 링크 소모 통합 테스트. 트랜잭션·롤백은 실제 PostgreSQL에서만 의미가 있어 Fake를 쓰지 않는다.
// 핸들러는 운영과 같은 DI 구성(AddInfrastructure)에서 꺼낸다 — 저장소·UnitOfWork가 같은 DbContext를 받는지도 함께 확인된다.
public sealed class ConsumeMagicLinkTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // 가입
    // - SignUp_CreatesUserAndRevokesRemainingLinks
    // - SignUp_WhenNicknameMissing_NeedsNicknameAndKeepsToken      (롤백으로 Q1 소모가 되돌아감 → 같은 토큰으로 재시도 성공)
    // - SignUp_WhenNicknameTaken_ReturnsConflictAndKeepsToken      (대소문자만 다른 닉네임 — ux_users_nickname_lower)
    // - SignUp_WhenNicknameFormatInvalid_ReturnsValidationAndKeepsToken
    // - SignUp_SavesGenderAndBirthdate / SignUp_WithoutBirthdate_SavesNull
    // - SignUp_WhenGenderMissingOrUnknown_ReturnsValidationAndKeepsToken   (R-US-04)
    // - SignUp_WhenBirthdateInvalid_ReturnsValidationAndKeepsToken         (R-US-05 형식·범위)
    //
    // 로그인
    // - SignIn_ExistingUser_IgnoresGenderAndBirthdate
    // - SignIn_ExistingUser_IgnoresNickname                        (기존 회원이 쓰레기 nickname을 보내도 로그인)
    // - SignIn_MatchesEmailIgnoringCase
    //
    // 무효 링크
    // - Consume_WhenTokenEmpty_ReturnsInvalidLink
    // - Consume_WhenTokenUnknown_ReturnsInvalidLink
    // - Consume_WhenReused_ReturnsInvalidLink
    // - Consume_WhenExpired_ReturnsInvalidLink                     (경계: 정확히 15분 = 만료, expires_at > now)

    // timestamptz는 마이크로초까지만 저장한다 — 초 단위 고정 시각을 쓴다.
    private static readonly DateTimeOffset IssuedAt = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
    private const string NewEmail = "new@nook.test";

    private readonly MutableClock _clock = new(IssuedAt);
    private readonly ServiceProvider _services;

    public ConsumeMagicLinkTests(PostgresFixture fixture) : base(fixture)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddScoped<ConsumeMagicLinkHandler>();
        _services = services.BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    // ── 가입 ──────────────────────────────────────────────

    [Fact]
    public async Task SignUp_CreatesUserAndRevokesRemainingLinks()
    {
        var first = await IssueAsync(NewEmail);
        var second = await IssueAsync(NewEmail);

        var outcome = (await ConsumeAsync(first, "누크")).Unwrap();

        Assert.False(outcome.NeedsNickname);
        var user = await Db.CreateContext().Users.SingleAsync();
        Assert.Equal(outcome.SignedInUserId, user.Id.Value);
        Assert.Equal("누크", user.Nickname.Value);
        Assert.NotNull(await ConsumedAtAsync(first));
        Assert.NotNull(await ConsumedAtAsync(second));   // Q3: 남은 링크도 무효
    }

    [Fact]
    public async Task SignUp_WhenNicknameMissing_NeedsNicknameAndKeepsToken()
    {
        var token = await IssueAsync(NewEmail);

        var outcome = (await ConsumeAsync(token, nickname: null)).Unwrap();

        Assert.True(outcome.NeedsNickname);
        Assert.Null(await ConsumedAtAsync(token));        // 롤백 → Q1 소모가 되돌아갔다
        Assert.Equal(0, await Db.CreateContext().Users.CountAsync());

        var retry = (await ConsumeAsync(token, "누크")).Unwrap();   // 같은 토큰 + 닉네임으로 재시도
        Assert.False(retry.NeedsNickname);
    }

    [Fact]
    public async Task SignUp_WhenNicknameTaken_ReturnsConflictAndKeepsToken()
    {
        await SeedUserAsync("nook", "old@nook.test");
        var token = await IssueAsync(NewEmail);

        var result = await ConsumeAsync(token, "NOOK");

        AssertFailure(result, UserErrors.NicknameTaken);
        Assert.Null(await ConsumedAtAsync(token));
        Assert.Equal(1, await Db.CreateContext().Users.CountAsync());
    }

    [Fact]
    public async Task SignUp_WhenNicknameFormatInvalid_ReturnsValidationAndKeepsToken()
    {
        var token = await IssueAsync(NewEmail);

        var result = await ConsumeAsync(token, "!");

        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(ErrorType.Validation, error.Type);   // 어느 닉네임 규칙인지는 Domain 테스트가 본다
        Assert.Null(await ConsumedAtAsync(token));
    }

    // ── 로그인 ────────────────────────────────────────────

    [Fact]
    public async Task SignUp_SavesGenderAndBirthdate()
    {
        var token = await IssueAsync(NewEmail);

        (await ConsumeAsync(token, "누크", "female", "1995-03-01")).Unwrap();

        var user = await Db.CreateContext().Users.SingleAsync();
        Assert.Equal((Gender.Female, new DateOnly(1995, 3, 1)), (user.Gender, user.Birthdate?.Value));
    }

    [Fact]
    public async Task SignUp_WithoutBirthdate_SavesNull()
    {
        var token = await IssueAsync(NewEmail);

        (await ConsumeAsync(token, "누크", "male", birthdate: null)).Unwrap();

        Assert.Null((await Db.CreateContext().Users.SingleAsync()).Birthdate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FEMALE")]     // 표기는 소문자만
    [InlineData("other")]
    public async Task SignUp_WhenGenderMissingOrUnknown_ReturnsValidationAndKeepsToken(string? gender)
    {
        var token = await IssueAsync(NewEmail);

        AssertFailure(await ConsumeAsync(token, "누크", gender), UserErrors.GenderInvalid);
        Assert.Null(await ConsumedAtAsync(token));
        Assert.Equal(0, await Db.CreateContext().Users.CountAsync());
    }

    // IssuedAt = 2026-10-20(UTC) → 오늘. 내일은 범위 밖.
    [Theory]
    [InlineData("95-03-01", "User.BirthdateFormat")]       // 원본 화면 표기 그대로 — FE가 바꿔 보내야 한다
    [InlineData("2026-02-30", "User.BirthdateFormat")]     // 없는 날짜
    [InlineData("1899-12-31", "User.BirthdateOutOfRange")]
    [InlineData("2026-10-21", "User.BirthdateOutOfRange")]
    public async Task SignUp_WhenBirthdateInvalid_ReturnsValidationAndKeepsToken(string birthdate, string code)
    {
        var token = await IssueAsync(NewEmail);

        var result = await ConsumeAsync(token, "누크", "none", birthdate);

        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(code, error.Code);
        Assert.Null(await ConsumedAtAsync(token));
    }

    [Fact]
    public async Task SignIn_ExistingUser_IgnoresGenderAndBirthdate()
    {
        await SeedUserAsync("누크", NewEmail);
        var token = await IssueAsync(NewEmail);

        var outcome = (await ConsumeAsync(token, nickname: null, gender: "invalid", birthdate: "nope")).Unwrap();

        Assert.False(outcome.NeedsNickname);
    }

    [Fact]
    public async Task SignIn_ExistingUser_IgnoresNickname()
    {
        var existing = await SeedUserAsync("누크", NewEmail);
        var token = await IssueAsync(NewEmail);

        var outcome = (await ConsumeAsync(token, "!")).Unwrap();

        Assert.Equal(existing.Id.Value, outcome.SignedInUserId);
        Assert.Equal(1, await Db.CreateContext().Users.CountAsync());
        Assert.NotNull(await ConsumedAtAsync(token));
    }

    [Fact]
    public async Task SignIn_MatchesEmailIgnoringCase()
    {
        var existing = await SeedUserAsync("누크", "Nook@Example.com");
        var token = await IssueAsync("nook@example.COM");

        var outcome = (await ConsumeAsync(token, nickname: null)).Unwrap();

        Assert.Equal(existing.Id.Value, outcome.SignedInUserId);
    }

    // ── 무효 링크 ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Consume_WhenTokenEmpty_ReturnsInvalidLink(string? token)
    {
        AssertFailure(await ConsumeAsync(token, "누크"), AuthErrors.InvalidLink);
    }

    [Fact]
    public async Task Consume_WhenTokenUnknown_ReturnsInvalidLink()
    {
        var (unknown, _) = MagicLinkSecret.Create();

        AssertFailure(await ConsumeAsync(unknown, "누크"), AuthErrors.InvalidLink);
    }

    [Fact]
    public async Task Consume_WhenReused_ReturnsInvalidLink()
    {
        var token = await IssueAsync(NewEmail);
        (await ConsumeAsync(token, "누크")).Unwrap();

        AssertFailure(await ConsumeAsync(token, "누크"), AuthErrors.InvalidLink);
    }

    [Fact]
    public async Task Consume_WhenExpired_ReturnsInvalidLink()
    {
        var token = await IssueAsync(NewEmail);
        _clock.Now = IssuedAt + MagicLinkPolicy.TokenLifetime;   // 정확히 만료 시각

        AssertFailure(await ConsumeAsync(token, "누크"), AuthErrors.InvalidLink);
    }

    // ── 도우미 ────────────────────────────────────────────

    // 요청마다 새 스코프 = 운영의 요청당 DbContext 하나와 같다.
    // 가입 폼은 성별을 반드시 보낸다 — 성별이 주제가 아닌 테스트는 기본값 "none".
    private async Task<Result<ConsumeMagicLinkOutcome>> ConsumeAsync(
        string? token, string? nickname, string? gender = "none", string? birthdate = null)
    {
        await using var scope = _services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ConsumeMagicLinkHandler>();
        return await handler.HandleAsync(new ConsumeMagicLinkCommand(token, nickname, gender, birthdate), CancellationToken.None);
    }

    // AU-01 발급과 같은 저장 경로(IMagicLinkTokenStore.AddAsync)로 넣고 원문을 돌려준다.
    private async Task<string> IssueAsync(string email)
    {
        var (raw, hash) = MagicLinkSecret.Create();
        await using var scope = _services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMagicLinkTokenStore>();
        await store.AddAsync(hash, Email.From(email), IssuedAt, IssuedAt + MagicLinkPolicy.TokenLifetime, CancellationToken.None);
        return raw;
    }

    private async Task<User> SeedUserAsync(string nickname, string email)
    {
        var user = User.Create(Nickname.From(nickname), Email.From(email), Gender.None, null);
        await using var db = Db.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<DateTimeOffset?> ConsumedAtAsync(string rawToken)
    {
        var hash = MagicLinkSecret.Hash(rawToken);
        await using var db = Db.CreateContext();
        return await db.Database.SqlQuery<DateTimeOffset?>(
            $"""SELECT consumed_at AS "Value" FROM magic_link_tokens WHERE token_hash = {hash}""").SingleAsync();
    }

    private static void AssertFailure<T>(Result<T> result, Error expected)
    {
        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(expected, error);
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
