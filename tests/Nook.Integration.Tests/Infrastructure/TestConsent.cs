using Nook.Domain.Users;

namespace Nook.Integration.Tests.Infrastructure;

// 테스트용 가입 동의(R-US-06). 필수만 = 성별·생년월일 없이, 선택 포함 = 성별·생년월일을 줄 때.
internal static class TestConsent
{
    private static readonly DateTimeOffset At = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    public static PrivacyConsent Required => PrivacyConsent.Agree(At, "test", optional: false);
    public static PrivacyConsent WithOptional => PrivacyConsent.Agree(At, "test", optional: true);
}
