using System.Diagnostics;
using Nook.Application.Reviews;
using Nook.Domain.Likes;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Likes;

// LK-01 리뷰에 좋아요를 누른다. UserId는 토큰의 sub, ReviewId는 경로 값.
public sealed record AddLikeCommand(Guid UserId, Guid ReviewId);

// A안(사용자 결정): 사전 확인 + 틈 사이 삭제만 23503 번역. BM과 달리 리뷰는 삭제될 수 있어 확인 뒤 INSERT 전 틈이 실제 경합이다.
// 사전 확인을 남긴 이유: 정상 404를 오류 경로에 태우지 않는다, D9 카운터로 트랜잭션에 묶여도 25P02(중단된 트랜잭션)가 정상 흐름에 안 생긴다.
public sealed class AddLikeHandler(
    IReviewRepository reviews,
    ILikeRepository likes,
    TimeProvider time)
{
    public async Task<Result<LikeView>> HandleAsync(AddLikeCommand command, CancellationToken ct)
    {
        var reviewId = ReviewId.From(command.ReviewId);
        if (!await reviews.ExistsAsync(reviewId, ct))
            return LikeErrors.ReviewNotFound;

        var like = Like.Create(UserId.From(command.UserId), reviewId, time.GetUtcNow());
        var outcome = await likes.TryAddAsync(like, ct);

        return outcome switch
        {
            AddLikeOutcome.Added => new LikeView(Liked: true, Changed: true),
            AddLikeOutcome.AlreadyLiked => new LikeView(Liked: true, Changed: false),
            // 틈 사이 삭제 — 사전 확인에서 없을 때와 회원에게는 같은 상황(사용자 결정).
            AddLikeOutcome.ReviewGone => LikeErrors.ReviewNotFound,
            _ => throw new UnreachableException($"처리하지 않은 {nameof(AddLikeOutcome)}: {outcome}"),
        };
    }
}
