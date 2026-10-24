using Nook.Application.Bookmarks;
using Nook.Application.Reviews;
using Nook.Domain.Places;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Places;

// PL-02(장소 부분) 장소 상세와 내 별을 조회한다. 리뷰 목록은 무한 스크롤이라 따로 받는다(GetPlaceReviews).
public sealed record GetPlaceQuery(Guid UserId, Guid PlaceId);

// PlaceView 필드 + 이 회원의 별(R-STAR). 상세 화면은 empty도 보여 준다(BM-03과 다른 점).
public sealed record PlaceDetailView(
    Guid Id,
    string KakaoPlaceId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string Star);

// 장소 조회 실패. 쓰기 쪽 PlaceNotFound(BookmarkErrors·ReviewWriteErrors)와 코드를 나눈 이유: 그쪽은 "쓸 대상이 없다"는 안내다.
public static class PlaceQueryErrors
{
    public static readonly Error PlaceNotFound = new(
        "Place.NotFound",
        "장소를 찾을 수 없습니다.",
        ErrorType.NotFound);
}

public sealed class GetPlaceHandler(IPlaceRepository places, IBookmarkRepository bookmarks, IReviewRepository reviews)
{
    public async Task<Result<PlaceDetailView>> HandleAsync(GetPlaceQuery query, CancellationToken ct)
    {
        // 경로의 Guid.Empty는 From이 예외 → 500(SaveBookmark와 같은 사용자 결정).
        var placeId = PlaceId.From(query.PlaceId);
        if (await places.GetAsync(placeId, ct) is not { } place)
            return PlaceQueryErrors.PlaceNotFound;

        var userId = UserId.From(query.UserId);
        var star = StarRule.Evaluate(
            await bookmarks.ExistsAsync(userId, placeId, ct),
            await reviews.ExistsAsync(userId, placeId, ct));

        return new PlaceDetailView(
            place.Id.Value,
            place.KakaoPlaceId.Value,
            place.Name,
            place.Address,
            place.Coordinate.Latitude,
            place.Coordinate.Longitude,
            star.ToString().ToLowerInvariant());
    }
}
