using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.Domain.Places;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Bookmarks;

// BM-02 장소 저장을 해제한다.
public sealed record RemoveBookmarkCommand(Guid UserId, Guid PlaceId);

public sealed class RemoveBookmarkHandler(
    IPlaceRepository places,
    IBookmarkRepository bookmarks,
    IReviewRepository reviews)
{
    public async Task<Result<BookmarkView>> HandleAsync(RemoveBookmarkCommand command, CancellationToken ct)
    {
        // 경로의 Guid.Empty는 From이 예외 → 500(사용자 결정: 정상 클라이언트가 보낼 수 없는 값).
        var placeId = PlaceId.From(command.PlaceId);
        if (!await places.ExistsAsync(placeId, ct))
            return BookmarkErrors.PlaceNotFound;

        var userId = UserId.From(command.UserId);
        var changed = await bookmarks.RemoveAsync(userId, placeId, ct);
        var hasReview = await reviews.ExistsAsync(userId, placeId, ct);

        return BookmarkView.From(bookmarked: false, changed, StarRule.Evaluate(isBookmarked: false, hasReview));
    }
}
