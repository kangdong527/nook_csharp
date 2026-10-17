using Nook.Domain.Groups;

namespace Nook.Domain.Tests.Groups;

// InviteCode.From — DB 값 복원 단위 테스트. 기준은 Nickname.From과 같다(빈 값만 막고, 규칙을 다시 검사하지 않고, 값을 바꾸지 않는다).
public class InviteCodeTests
{
    // 테스트 목록
    //
    // From — 복원
    // - From_WhenValueIsEmptyOrWhiteSpace_Throws     ("", " ", "\t")
    // - From_WhenValueBreaksCreateRules_Restores      (3자 소문자)
    // - From_KeepsValueAsIs                           (대문자 변환 없음 — Create는 대문자로 바꾼다)

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void From_WhenValueIsEmptyOrWhiteSpace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => InviteCode.From(value));
    }

    [Fact]
    public void From_WhenValueBreaksCreateRules_Restores()
    {
        var value = "abc";
        Assert.False(InviteCode.Create(value).TryGetValue(out _, out _));   // 전제 확인: Create는 거절하는 값

        Assert.Equal(value, InviteCode.From(value).Value);
    }

    [Fact]
    public void From_KeepsValueAsIs()
    {
        const string value = "abcdef";

        Assert.Equal(value, InviteCode.From(value).Value);
    }
}
