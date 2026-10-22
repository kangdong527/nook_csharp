using Nook.Application.Reviews;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Likes;

// LK-01 좋아요를 취소한다. 없는 리뷰는 404(사용자 결정) — DELETE는 FK 검사가 없어 0행만으로는 "없는 리뷰"와 "이미 취소"를 못 가른다.
public sealed record RemoveLikeCommand(Guid UserId, Guid ReviewId);

public sealed class RemoveLikeHandler(
    IReviewRepository reviews,
    ILikeRepository likes)
{
    public async Task<Result<LikeView>> HandleAsync(RemoveLikeCommand command, CancellationToken ct)
    {
        var reviewId = ReviewId.From(command.ReviewId);
        if (!await reviews.ExistsAsync(reviewId, ct))
            return LikeErrors.ReviewNotFound;

        var userId = UserId.From(command.UserId);
        var changed = await likes.RemoveAsync(userId, reviewId, ct);

        // 확인 뒤 리뷰가 삭제되면 CASCADE가 like 행을 먼저 지워 0행 → 200 changed=false. 번역하지 않는다(사용자 결정):
        // Add의 ReviewGone과 달리 liked=false는 사실이다. changed=false("원래 없었다")만 엄밀히는 부정확하다.
        return new LikeView(Liked: false, Changed: changed);
    }
}
