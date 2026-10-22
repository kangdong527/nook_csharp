using Nook.SharedKernel;

namespace Nook.Application.Likes;

public static class LikeErrors
{
    // LK-01. 사전 확인에서 없거나, 확인과 INSERT 사이에 삭제됨(RV-03·탈퇴 CASCADE) — 회원에게는 같은 상황이라 같은 오류.
    public static readonly Error ReviewNotFound = new(
        "Like.ReviewNotFound",
        "좋아요할 리뷰를 찾을 수 없습니다.",
        ErrorType.NotFound);
}
