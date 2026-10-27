using Nook.Application.Places;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// RV-04 수정할 내 리뷰를 불러온다. RV-02 수정 화면 전용 — 작성자만 받는다(FE-1 사용자 결정).
public sealed record GetReviewForEditQuery(Guid UserId, Guid ReviewId);

// 수정 화면이 그리는 것만. Images·Version의 의미는 ReviewView와 같다(Id는 RV-02 KeepImageIds로, Version은 그대로 돌려보낸다).
public sealed record EditableReviewView(
    Guid Id,
    Guid PlaceId,
    string PlaceName,
    string Content,
    IReadOnlyList<ReviewImageView> Images,
    int Version);

public sealed class GetReviewForEditHandler(IReviewRepository reviews, IPlaceRepository places)
{
    public async Task<Result<EditableReviewView>> HandleAsync(GetReviewForEditQuery query, CancellationToken ct)
    {
        // RV-02 4단계와 같은 순서: 없음(404) → 남의 것(403). GetAsync는 추적 쿼리지만 한 건이라 비용이 작아 재사용한다.
        var review = await reviews.GetAsync(ReviewId.From(query.ReviewId), ct);
        if (review is null)
            return ReviewWriteErrors.ReviewNotFound;

        if (review.UserId != UserId.From(query.UserId))
            return ReviewWriteErrors.NotReviewAuthor;

        // 장소는 삭제하지 않고 fk_reviews_places는 RESTRICT — 여기서 null이면 데이터가 깨진 것이라 예외(500).
        var place = await places.GetAsync(review.PlaceId, ct)
            ?? throw new InvalidOperationException($"리뷰 {review.Id.Value}의 장소 {review.PlaceId.Value}가 없습니다.");

        return new EditableReviewView(
            review.Id.Value,
            review.PlaceId.Value,
            place.Name,
            review.Content.Value,
            [.. review.Images.Select(i => new ReviewImageView(i.Id.Value, i.StorageKey))],
            review.Version);
    }
}
