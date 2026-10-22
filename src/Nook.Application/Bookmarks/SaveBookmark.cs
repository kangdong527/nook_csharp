using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Bookmarks;

// BM-01 장소를 저장한다. UserId는 토큰의 sub(Api가 꺼냄), PlaceId는 경로 값 — 둘 다 원시 Guid로 받는다.
public sealed record SaveBookmarkCommand(Guid UserId, Guid PlaceId);

// 트랜잭션 없음: 쓰기는 INSERT 한 문장. 장소 확인·리뷰 확인은 읽기다.
public sealed class SaveBookmarkHandler(
    IPlaceRepository places,
    IBookmarkRepository bookmarks,
    IReviewRepository reviews,
    TimeProvider time)
{
    public async Task<Result<BookmarkView>> HandleAsync(SaveBookmarkCommand command, CancellationToken ct)
    {
        // 경로의 Guid.Empty는 From이 예외 → 500(사용자 결정: 정상 클라이언트가 보낼 수 없는 값).
        var placeId = PlaceId.From(command.PlaceId);
        if (!await places.ExistsAsync(placeId, ct))
            return BookmarkErrors.PlaceNotFound;

        var userId = UserId.From(command.UserId);
        var changed = await bookmarks.TryAddAsync(Bookmark.Create(userId, placeId, time.GetUtcNow()), ct);
        var hasReview = await reviews.ExistsAsync(userId, placeId, ct);

        // changed와 무관하게 이 시점의 북마크 상태는 true — 0행(이미 저장됨·경합에서 짐)도 행은 있다.
        return BookmarkView.From(bookmarked: true, changed, StarRule.Evaluate(isBookmarked: true, hasReview));
    }
}
