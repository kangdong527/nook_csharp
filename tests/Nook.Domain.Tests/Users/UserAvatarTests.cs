using Nook.Domain.Tests;
using Nook.Domain.Users;

namespace Nook.Domain.Tests.Users;

// User.ReplaceAvatar(PF-01) — 이전 키를 돌려줘 호출 쪽이 커밋 뒤 지우게 한다.
public class UserAvatarTests
{
    // 테스트 목록
    //
    // - ReplaceAvatar_ReturnsPreviousKey              (없음 → a: null, a → b: a, b → null: b)
    // - ReplaceAvatar_SameKey_ReturnsNull             (같은 키를 다시 넣어도 지울 것이 없다 — 자기 사진을 지우는 사고 방지)
    // - ReplaceAvatar_WithBrokenKey_Throws            (빈 값·상한 초과 — 서버가 만든 키라 버그)

    private const string A = "avatars/2026/10/27/0123456789abcdef0123456789abcdef.png";
    private const string B = "avatars/2026/10/27/fedcba9876543210fedcba9876543210.png";

    private static User NewUser() => User.Create(Nickname.From("누크"), Email.From("nook@example.com"), TestConsent.Required);

    [Fact]
    public void ReplaceAvatar_ReturnsPreviousKey()
    {
        var user = NewUser();

        Assert.Null(user.ReplaceAvatar(A));
        Assert.Equal(A, user.ReplaceAvatar(B));
        Assert.Equal(B, user.ReplaceAvatar(null));
        Assert.Null(user.AvatarKey);
    }

    [Fact]
    public void ReplaceAvatar_SameKey_ReturnsNull()
    {
        var user = NewUser();
        user.ReplaceAvatar(A);

        Assert.Null(user.ReplaceAvatar(A));
        Assert.Equal(A, user.AvatarKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("avatars/2026/10/27/0123456789abcdef0123456789abcdef.webpX")]   // 57자
    public void ReplaceAvatar_WithBrokenKey_Throws(string key)
    {
        Assert.ThrowsAny<ArgumentException>(() => NewUser().ReplaceAvatar(key));
    }
}
