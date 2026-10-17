using Nook.Domain.Users;

namespace Nook.Domain.Tests.Users;

// Email.From — DB 값 복원 단위 테스트. 기준은 Nickname.From과 같다.
public class EmailTests
{
    // 테스트 목록
    //
    // From — 복원
    // - From_WhenValueIsEmptyOrWhiteSpace_Throws     ("", " ", "\t")
    // - From_WhenValueBreaksCreateRules_Restores      ('@' 없음)
    // - From_KeepsValueAsIs                           (대소문자 변환 없음)

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void From_WhenValueIsEmptyOrWhiteSpace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Email.From(value));
    }

    [Fact]
    public void From_WhenValueBreaksCreateRules_Restores()
    {
        const string value = "no-at-sign";
        Assert.False(Email.Create(value).TryGetValue(out _, out _));   // 전제 확인: Create는 거절하는 값

        var email = Email.From(value);

        Assert.Equal(value, email.Value);
    }

    [Fact]
    public void From_KeepsValueAsIs()
    {
        const string value = "Nook@Example.com";

        Assert.Equal(value, Email.From(value).Value);
    }
}
