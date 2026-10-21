using Nook.Domain.Stars;

namespace Nook.Domain.Tests.Stars;

// R-STAR 표(usecases.md) 4행을 그대로 옮긴다. 표가 바뀌면 이 InlineData도 함께 바뀐다.
public class StarRuleTests
{
    // 테스트 목록
    // - Evaluate_FollowsRStarTable   (북마크 × 리뷰 4조합)

    [Theory]
    [InlineData(false, false, StarState.Empty)]
    [InlineData(true, false, StarState.Gray)]
    [InlineData(false, true, StarState.Fill)]
    [InlineData(true, true, StarState.Fill)]    // 리뷰가 북마크보다 우선
    public void Evaluate_FollowsRStarTable(bool isBookmarked, bool hasReview, StarState expected)
    {
        Assert.Equal(expected, StarRule.Evaluate(isBookmarked, hasReview));
    }
}
