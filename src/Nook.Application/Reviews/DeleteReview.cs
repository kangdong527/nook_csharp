using Microsoft.Extensions.Logging;
using Nook.Application.Bookmarks;
using Nook.Application.Images;
using Nook.Domain.Reviews;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// RV-03 리뷰를 삭제한다. UserId는 토큰의 sub, ReviewId는 경로.
public sealed record DeleteReviewCommand(Guid UserId, Guid ReviewId);

// RV-03 응답(ADR-009 결정 3). Changed: 이번 요청이 지웠는가. Star: 지웠으면 이 회원·장소의 별, 0행이면 null(장소를 알 수 없다).
// 프론트는 Changed=false면 별을 갱신하지 않는다.
public sealed record DeleteReviewView(bool Changed, string? Star)
{
    internal static DeleteReviewView Deleted(StarState star) => new(true, star.ToString().ToLowerInvariant());

    internal static readonly DeleteReviewView Unchanged = new(false, null);
}

// 트랜잭션 경계: DeleteAsync 한 문장(자동 커밋)이 리뷰·이미지 행·좋아요를 함께 지운다(CASCADE). 스토리지 삭제는 그 뒤(ADR-009 결정 1).
public sealed class DeleteReviewHandler(
    IReviewRepository reviews,
    IBookmarkRepository bookmarks,
    IImageStorage storage,
    ILogger<DeleteReviewHandler> logger)
{
    public async Task<Result<DeleteReviewView>> HandleAsync(DeleteReviewCommand command, CancellationToken ct)
    {
        // 경로의 Guid.Empty는 From이 예외 → 500(SaveBookmark와 같은 사용자 결정).
        var reviewId = ReviewId.From(command.ReviewId);
        var userId = UserId.From(command.UserId);

        // 2단계. 판정은 이 한 문장의 영향 행 수다 — 아래 재조회는 0행의 "이유"만 가린다(ADR-009 결정 3).
        var deletion = await reviews.DeleteAsync(reviewId, userId, ct);

        // 3단계(3a′·3b′). 0행 = 없거나(멱등 성공) 남의 것(403).
        if (deletion == null)
        {
            if (await reviews.ExistsAsync(reviewId, ct))
                return ReviewWriteErrors.NotReviewAuthor;
            else
                return DeleteReviewView.Unchanged;
        }

        // DeleteAsync는 자동 커밋이라 여기는 커밋 뒤다. 별 계산보다 먼저 — 별 조회가 실패(500)해도 정리는 끝나 있다.
        await ReviewImageCleanup.DeleteAfterCommitAsync(storage, deletion.StorageKeys, logger);

        // 4단계. 커밋 뒤에 세므로 방금 지운 리뷰는 빠진다. 같은 장소의 다른 날 리뷰(R-RV-03은 하루 1개)가 남으면 fill 유지.
        var isBookmarked = await bookmarks.ExistsAsync(userId, deletion.PlaceId, ct);
        var hasReview = await reviews.ExistsAsync(userId, deletion.PlaceId, ct);
        return DeleteReviewView.Deleted(StarRule.Evaluate(isBookmarked, hasReview));
    }
}
