using Nook.Domain.Reviews;

namespace Nook.Domain.Tests.Reviews;

// ReviewContent.From — DB 값 복원 단위 테스트. 기준은 Nickname.From과 같다(빈 값만 막고, 규칙을 다시 검사하지 않고, 값을 바꾸지 않는다).
public class ReviewContentTests
{
    // 테스트 목록
    //
    // From — 복원
    // - From_WhenValueIsEmptyOrWhiteSpace_Throws     ("", " ", "\t")
    // - From_WhenValueBreaksCreateRules_Restores      (5자 — 하한 미만)
    // - From_KeepsValueAsIs                           (그대로)

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void From_WhenValueIsEmptyOrWhiteSpace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => ReviewContent.From(value));
    }

    [Fact]
    public void From_WhenValueBreaksCreateRules_Restores()
    {
        var value = "짧은 리뷰";
        Assert.False(ReviewContent.Create(value).TryGetValue(out _, out _));   // 전제 확인: Create는 거절하는 값

        Assert.Equal(value, ReviewContent.From(value).Value);
    }

    [Fact]
    public void From_KeepsValueAsIs()
    {
        const string value = "분위기가 좋고 커피가 맛있어요";

        Assert.Equal(value, ReviewContent.From(value).Value);
    }
}
