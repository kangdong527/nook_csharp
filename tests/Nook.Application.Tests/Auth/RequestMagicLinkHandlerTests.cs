using Nook.Application.Auth;
using Nook.Domain.Users;

namespace Nook.Application.Tests.Auth;

// RequestMagicLinkHandler — AU-01 흐름 1 단위 테스트. 저장소·큐·시계는 Fake로 바꾼다(DB·메일 없음).
public class RequestMagicLinkHandlerTests
{
    // 테스트 목록
    //
    // 형식 검사
    // - Handle_WhenEmailFormatInvalid_ReturnsValidationWithoutTouchingStore   (null, "", "abc")
    //
    // Q2 속도 제한 (경계: MaxIssuesPerWindow = 3)
    // - Handle_WhenIssuedBelowLimit_IssuesToken                              (2개 발급 상태)
    // - Handle_WhenIssuedAtLimit_ReturnsSuccessWithoutIssuing                (3개 발급 상태)
    //
    // 발급
    // - Handle_UsesSingleNowForWindowCreatedAndExpiry                        (since = now-15분, expires = now+15분)
    // - Handle_MailsRawTokenWhoseHashIsStored                                (메일 원문의 해시 = 저장된 해시)

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTokenStore _store = new();
    private readonly FakeMailQueue _mail = new();
    private readonly RequestMagicLinkHandler _sut;

    public RequestMagicLinkHandlerTests()
    {
        _sut = new RequestMagicLinkHandler(_store, _mail, new TickingTimeProvider(Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public async Task Handle_WhenEmailFormatInvalid_ReturnsValidationWithoutTouchingStore(string? email)
    {
        var result = await _sut.HandleAsync(new RequestMagicLinkCommand(email), CancellationToken.None);

        Assert.True(result.IsFailure(out var error));
        Assert.Equal(UserErrors.EmailFormat, error);
        Assert.Equal(0, _store.CountCalls);   // 싼 검사 먼저: DB를 보지 않는다
        Assert.Empty(_store.Added);
        Assert.Empty(_mail.Sent);
    }

    [Fact]
    public async Task Handle_WhenIssuedBelowLimit_IssuesToken()
    {
        _store.IssuedCount = MagicLinkPolicy.MaxIssuesPerWindow - 1;

        var result = await _sut.HandleAsync(new RequestMagicLinkCommand("a@b.c"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_store.Added);
        Assert.Single(_mail.Sent);
    }

    [Fact]
    public async Task Handle_WhenIssuedAtLimit_ReturnsSuccessWithoutIssuing()
    {
        _store.IssuedCount = MagicLinkPolicy.MaxIssuesPerWindow;

        var result = await _sut.HandleAsync(new RequestMagicLinkCommand("a@b.c"), CancellationToken.None);

        Assert.True(result.IsSuccess);   // 항상 같은 응답(D5 결정): 제한에 걸린 것이 드러나지 않는다
        Assert.Empty(_store.Added);
        Assert.Empty(_mail.Sent);
    }

    [Fact]
    public async Task Handle_UsesSingleNowForWindowCreatedAndExpiry()
    {
        await _sut.HandleAsync(new RequestMagicLinkCommand("a@b.c"), CancellationToken.None);

        Assert.Equal(Now - MagicLinkPolicy.RateLimitWindow, _store.LastSince);
        var added = Assert.Single(_store.Added);
        Assert.Equal(Now, added.CreatedAt);
        Assert.Equal(Now + MagicLinkPolicy.TokenLifetime, added.ExpiresAt);
    }

    [Fact]
    public async Task Handle_MailsRawTokenWhoseHashIsStored()
    {
        await _sut.HandleAsync(new RequestMagicLinkCommand(" a@b.c "), CancellationToken.None);

        var added = Assert.Single(_store.Added);
        var sent = Assert.Single(_mail.Sent);
        Assert.Equal(added.TokenHash, MagicLinkSecret.Hash(sent.RawToken));   // AU-02가 같은 함수로 찾아낸다
        Assert.Equal("a@b.c", added.Email.Value);                             // 정규화된 이메일로 저장
        Assert.Equal(added.Email, sent.Email);
    }

    private sealed record AddedToken(byte[] TokenHash, Email Email, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

    private sealed class FakeTokenStore : IMagicLinkTokenStore
    {
        public int IssuedCount { get; set; }
        public int CountCalls { get; private set; }
        public DateTimeOffset? LastSince { get; private set; }
        public List<AddedToken> Added { get; } = [];

        public Task<int> CountIssuedSinceAsync(Email email, DateTimeOffset since, CancellationToken ct)
        {
            CountCalls++;
            LastSince = since;
            return Task.FromResult(IssuedCount);
        }

        public Task AddAsync(byte[] tokenHash, Email email, DateTimeOffset createdAt, DateTimeOffset expiresAt, CancellationToken ct)
        {
            Added.Add(new AddedToken(tokenHash, email, createdAt, expiresAt));
            return Task.CompletedTask;
        }

        // AU-01은 소모·무효화를 부르지 않는다. 불리면 핸들러가 잘못된 것이다.
        public Task<Email?> ConsumeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task DeleteAllAsync(Email email, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task RevokeRemainingAsync(Email email, DateTimeOffset now, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMailQueue : IMagicLinkMailQueue
    {
        public List<(Email Email, string RawToken)> Sent { get; } = [];

        public void Enqueue(Email email, string rawToken) => Sent.Add((email, rawToken));
    }

    // 부를 때마다 1초씩 간다. 첫 호출이 Now. 핸들러가 시각을 두 번 구하면 since·created·expires가 어긋나 테스트가 잡는다
    // (고정 시계는 몇 번을 불러도 같은 값이라 이 실수를 못 잡는다).
    private sealed class TickingTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private int _calls;

        public override DateTimeOffset GetUtcNow() => start.AddSeconds(_calls++);
    }
}
