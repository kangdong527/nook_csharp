using Nook.Infrastructure.Auth;

namespace Nook.Integration.Tests.Auth;

// A1 수신자별 발송 상한(DEP 7단계). 순수 메모리 로직이라 DB 없이 돈다. internal 이라 통합 테스트 프로젝트에 둔다(InternalsVisibleTo).
public sealed class RecipientSendLimiterTests
{
    // 테스트 목록
    //
    // - AllowsUpToLimit_ThenRejects
    // - SlidingWindow_AllowsAgainOnlyAsOldestSendExpires   (고정 창이 아니다 — 경계 앞뒤로 몰아도 2배가 안 나간다)
    // - Recipient_IsCaseInsensitive                        (R-US-03)
    // - DifferentRecipients_HaveSeparateLimits
    // - Sweep_ForgetsRecipientsOutsideWindow               (이메일을 바꿔 가는 공격에도 기록이 묶인다)

    private const int Limit = 3;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly DateTimeOffset T0 = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AllowsUpToLimit_ThenRejects()
    {
        var limiter = new RecipientSendLimiter(Limit, Window);

        var results = Enumerable.Range(0, Limit + 1).Select(_ => limiter.TryAcquire("a@nook.test", T0)).ToList();

        Assert.Equal([true, true, true, false], results);
    }

    [Fact]
    public void SlidingWindow_AllowsAgainOnlyAsOldestSendExpires()
    {
        var limiter = new RecipientSendLimiter(Limit, Window);
        limiter.TryAcquire("a@nook.test", T0);
        limiter.TryAcquire("a@nook.test", T0.AddMinutes(5));
        limiter.TryAcquire("a@nook.test", T0.AddMinutes(10));

        Assert.False(limiter.TryAcquire("a@nook.test", T0.AddMinutes(14)));   // 창 안에 3통
        Assert.True(limiter.TryAcquire("a@nook.test", T0.AddMinutes(15)));    // T0 발송만 창 밖 → 한 자리
        Assert.False(limiter.TryAcquire("a@nook.test", T0.AddMinutes(15)));   // 고정 창이었다면 여기서 새 창 3통이 열린다
    }

    [Fact]
    public void Recipient_IsCaseInsensitive()
    {
        var limiter = new RecipientSendLimiter(Limit, Window);
        limiter.TryAcquire("a@nook.test", T0);
        limiter.TryAcquire("A@Nook.Test", T0);
        limiter.TryAcquire("A@NOOK.TEST", T0);

        Assert.False(limiter.TryAcquire("a@NOOK.test", T0));
    }

    [Fact]
    public void DifferentRecipients_HaveSeparateLimits()
    {
        var limiter = new RecipientSendLimiter(Limit, Window);
        for (var i = 0; i < Limit; i++)
            limiter.TryAcquire("a@nook.test", T0);

        Assert.True(limiter.TryAcquire("b@nook.test", T0));
    }

    [Fact]
    public void Sweep_ForgetsRecipientsOutsideWindow()
    {
        var limiter = new RecipientSendLimiter(Limit, Window);
        for (var i = 0; i < 300; i++)                       // 호출 1~300(256번째에 쓸기 — 아직 다 창 안)
            limiter.TryAcquire($"old{i}@nook.test", T0);

        var later = T0 + Window;
        for (var i = 0; i < 212; i++)                       // 호출 301~512: 512번째 호출 직전 쓸기 → old 300명 정리
            limiter.TryAcquire($"new{i}@nook.test", later);

        Assert.Equal(212, limiter.TrackedRecipients);
    }
}
