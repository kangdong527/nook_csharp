using Nook.Domain.Groups;

namespace Nook.Domain.Tests.Groups;

// GroupName.From — DB 값 복원 단위 테스트. 기준은 Nickname.From과 같다(빈 값만 막고, 규칙을 다시 검사하지 않고, 값을 바꾸지 않는다).
public class GroupNameTests
{
    // 테스트 목록
    //
    // From — 복원
    // - From_WhenValueIsEmptyOrWhiteSpace_Throws     ("", " ", "\t")
    // - From_WhenValueBreaksCreateRules_Restores      (1자 — 하한 미만)
    // - From_KeepsValueAsIs                           (가운데 공백 유지)

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void From_WhenValueIsEmptyOrWhiteSpace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => GroupName.From(value));
    }

    [Fact]
    public void From_WhenValueBreaksCreateRules_Restores()
    {
        var value = "a";
        Assert.False(GroupName.Create(value).TryGetValue(out _, out _));   // 전제 확인: Create는 거절하는 값

        Assert.Equal(value, GroupName.From(value).Value);
    }

    [Fact]
    public void From_KeepsValueAsIs()
    {
        const string value = "주말 모임";

        Assert.Equal(value, GroupName.From(value).Value);
    }
}
