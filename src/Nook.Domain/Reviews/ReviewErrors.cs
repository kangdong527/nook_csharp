using Nook.Domain.Common;

namespace Nook.Domain.Reviews;

// 리뷰 애그리거트의 실패 카탈로그. Code는 클라이언트와의 계약이므로 한 번 공개한 뒤에는 바꾸지 않는다.
public static class ReviewErrors
{
    public static readonly Error ContentLength = new(
        "Review.ContentLength",
        $"리뷰 본문은 앞뒤 공백을 제외하고 {ReviewContent.MinLength}~{ReviewContent.MaxLength}자여야 합니다.",
        ErrorType.Validation);
}
