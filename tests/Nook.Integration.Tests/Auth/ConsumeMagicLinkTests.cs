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
    // - SignUp_WithoutGender_SavesNull                                     (R-US-04 선택, 2026-11-09)
    // - SignUp_WhenGenderUnknown_ReturnsValidationAndKeepsToken
    // - SignUp_WithoutRequiredConsent_ReturnsValidationAndKeepsToken       (R-US-06 필수 동의 둘)
    // - SignUp_OptionalItemsWithoutOptionalConsent_ReturnsValidation       (R-US-06 선택 동의)
    // - SignUp_WhenBirthdateUnderMinimumAge_ReturnsValidation              (만 14세 경계)
    // - SignUp_RecordsConsentWithServerPolicyVersion                       (버전은 서버가 정한다)
    // - SignUp_WhenBirthdateInvalid_ReturnsValidationAndKeepsToken         (R-US-05 형식·범위)
    //
    // 로그인
    // - SignIn_ExistingUser_IgnoresGenderAndBirthdate
    // - SignIn_ExistingUser_DoesNotRequireConsent                  (동의는 가입 때만)
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
    public async Task SignUp_WithoutGender_SavesNull(string? gender)
    {
        var token = await IssueAsync(NewEmail);

        (await ConsumeAsync(token, "누크", gender)).Unwrap();

        Assert.Null((await Db.CreateContext().Users.SingleAsync()).Gender);
    }

    [Theory]
    [InlineData("FEMALE")]     // 표기는 소문자만
    [InlineData("other")]
    public async Task SignUp_WhenGenderUnknown_ReturnsValidationAndKeepsToken(string? gender)
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

    // ── 가입 동의(R-US-06) ─────────────────────────────────

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task SignUp_WithoutRequiredConsent_ReturnsValidationAndKeepsToken(bool agreePrivacy, bool confirmAge14)
    {
        var token = await IssueAsync(NewEmail);

        AssertFailure(await ConsumeAsync(token, "누크", agreePrivacy: agreePrivacy, confirmAge14: confirmAge14), UserErrors.ConsentRequired);
        Assert.Null(await ConsumedAtAsync(token));
        Assert.Equal(0, await Db.CreateContext().Users.CountAsync());
    }

    [Theory]
    [InlineData("female", null)]
    [InlineData(null, "1995-03-01")]
    public async Task SignUp_OptionalItemsWithoutOptionalConsent_ReturnsValidation(string? gender, string? birthdate)
    {
        var token = await IssueAsync(NewEmail);

        AssertFailure(await ConsumeAsync(token, "누크", gender, birthdate, agreeOptional: false), UserErrors.OptionalConsentRequired);
        Assert.Equal(0, await Db.CreateContext().Users.CountAsync());
    }

    // IssuedAt = 2026-10-20 → 2012-10-20 생은 오늘 만 14세, 2012-10-21 생은 13세
    [Theory]
    [InlineData("2012-10-21", false)]
    [InlineData("2012-10-20", true)]
    public async Task SignUp_WhenBirthdateUnderMinimumAge_ReturnsValidation(string birthdate, bool accepted)
    {
        var token = await IssueAsync(NewEmail);

        var result = await ConsumeAsync(token, "누크", birthdate: birthdate);

        if (accepted)
            result.Unwrap();
        else
            AssertFailure(result, UserErrors.UnderMinimumAge);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignUp_RecordsConsentWithServerPolicyVersion(bool withOptional)
    {
        var token = await IssueAsync(NewEmail);

        (await ConsumeAsync(token, "누크", gender: withOptional ? "none" : null)).Unwrap();

        var user = await Db.CreateContext().Users.SingleAsync();
        Assert.Equal(PrivacyPolicy.CurrentVersion, user.PrivacyPolicyVersion);
        Assert.Equal(IssuedAt, user.PrivacyConsentedAt);
        Assert.Equal(withOptional ? IssuedAt : null, user.OptionalConsentedAt);
    }

    [Fact]
    public async Task SignIn_ExistingUser_DoesNotRequireConsent()
    {
        await SeedUserAsync("누크", NewEmail);
        var token = await IssueAsync(NewEmail);

        var outcome = (await ConsumeAsync(token, nickname: null, agreePrivacy: false, confirmAge14: false)).Unwrap();

        Assert.False(outcome.NeedsNickname);
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
    // 기본: 필수 동의 둘 다 체크(가입 폼은 체크해야 제출된다), 성별·생년월일 없음.
    // agreeOptional 을 주지 않으면 선택 항목을 보낼 때만 켠다 — 동의가 주제가 아닌 테스트가 동의를 신경 쓰지 않게.
    private async Task<Result<ConsumeMagicLinkOutcome>> ConsumeAsync(
        string? token, string? nickname, string? gender = null, string? birthdate = null,
        bool agreePrivacy = true, bool confirmAge14 = true, bool? agreeOptional = null)
    {
        await using var scope = _services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ConsumeMagicLinkHandler>();
        var optional = agreeOptional ?? (!string.IsNullOrEmpty(gender) || !string.IsNullOrEmpty(birthdate));
        return await handler.HandleAsync(
            new ConsumeMagicLinkCommand(token, nickname, gender, birthdate, agreePrivacy, confirmAge14, optional), CancellationToken.None);
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
        var user = User.Create(Nickname.From(nickname), Email.From(email), TestConsent.Required);
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
