using Nook.Domain.Stars;
using Nook.Domain.Users;

namespace Nook.Application.Bookmarks;

// BM-03 내 지도(저장·리뷰 장소)를 조회한다. 읽기 전용 — 실패 흐름이 없어 Result를 쓰지 않는다.
public sealed record GetMyPlacesQuery(Guid UserId);

// 장소 필드는 PlaceView와 같고 Star를 더한다. Star는 gray(저장만)·fill(리뷰 있음) — 목록에 오른 장소는 정의상 empty가 아니다.
// 별 판정은 서버가 한다(R-STAR를 한 곳에) — 클라이언트는 표시만.
public sealed record MyPlaceView(
    Guid Id,
    string KakaoPlaceId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    string Star)
{
    internal static MyPlaceView From(MyPlace myPlace) => new(
        myPlace.Place.Id.Value,
        myPlace.Place.KakaoPlaceId.Value,
        myPlace.Place.Name,
        myPlace.Place.Address,
        myPlace.Place.Coordinate.Latitude,
        myPlace.Place.Coordinate.Longitude,
        StarRule.Evaluate(myPlace.IsBookmarked, myPlace.HasReview).ToString().ToLowerInvariant());
}

public sealed class GetMyPlacesHandler(IBookmarkRepository bookmarks)
{
    public async Task<IReadOnlyList<MyPlaceView>> HandleAsync(GetMyPlacesQuery query, CancellationToken ct)
    {
        var myPlaces = await bookmarks.ListMyPlacesAsync(UserId.From(query.UserId), ct);
        return [.. myPlaces.Select(MyPlaceView.From)];
    }
}
