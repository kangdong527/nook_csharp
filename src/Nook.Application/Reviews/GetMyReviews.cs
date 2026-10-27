using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// PF-04 내 리뷰 피드 — 최신순, "5개씩 더 보기"(유스케이스 PF-04). 커서는 PL-02와 같은 키셋(ReviewCursor).
public sealed record GetMyReviewsQuery(Guid UserId, string? Cursor, int? Limit);

public sealed record MyReviewPageView(IReadOnlyList<MyReviewView> Items, string? NextCursor);

// 프로필 화면의 리뷰 카드는 장소 이름을 함께 보여 준다(원본 showPlaceName). 작성자는 나라서 넣지 않는다.
public sealed record MyReviewView(
    Guid Id,
    Guid PlaceId,
    string PlaceName,
    string Content,
    IReadOnlyList<ReviewImageView> Images,
    DateTimeOffset CreatedAt,
    int Version,
    int LikeCount,
    bool LikedByMe);

public sealed class GetMyReviewsHandler(IReviewRepository reviews, IReviewImageUrls urls)
{
    public const int DefaultLimit = 5;

    public async Task<Result<MyReviewPageView>> HandleAsync(GetMyReviewsQuery query, CancellationToken ct)
    {
        var limit = query.Limit ?? DefaultLimit;
        if (limit is < 1 or > GetPlaceReviewsHandler.MaxLimit)
            return ReviewQueryErrors.InvalidLimit;

        ReviewCursor? after = null;
        if (query.Cursor is not null && !ReviewCursor.TryDecode(query.Cursor, out after))
            return ReviewQueryErrors.InvalidCursor;

        // 하나 더 읽어 다음 페이지가 있는지 안다 — COUNT 없이(PL-02와 같다).
        var rows = await reviews.ListByUserAsync(UserId.From(query.UserId), after, limit + 1, ct);
        var page = rows.Take(limit).ToList();

        var nextCursor = rows.Count > limit
            ? new ReviewCursor(page[^1].Review.CreatedAt, page[^1].Review.Id.Value).Encode()
            : null;

        return new MyReviewPageView([.. page.Select(ToView)], nextCursor);
    }

    private MyReviewView ToView(MyReview row) => new(
        row.Review.Id.Value,
        row.Review.PlaceId.Value,
        row.PlaceName,
        row.Review.Content.Value,
        [.. row.Review.Images.Select(i => ReviewImageView.From(i.Id.Value, i.StorageKey, urls))],
        row.Review.CreatedAt,
        row.Review.Version,
        row.LikeCount,
        row.LikedByMe);
}
