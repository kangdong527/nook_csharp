
namespace Nook.Domain.Stars;

public static class StarRule
{
    // 별 상태 계산 로직
    public static StarState Evaluate(bool isBookmarked, bool hasReview)
    {
        if (hasReview) return StarState.Fill;    // 리뷰가 있으면 북마크와 무관하게 채운별 (O,O / X,O)
        if (isBookmarked) return StarState.Gray; // 북마크만 있으면 회색별 (O,X)
        return StarState.Empty;                  // 둘 다 없으면 표시 안 함 (X,X)
    }
}
