using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Users;

namespace Nook.Application.Bookmarks;

// 북마크 저장 담당(erd.md bookmarks). R-BM-01(회원·장소당 0/1건)은 pk_bookmarks가 최종 보장한다.
// 반환값은 "이번 요청이 상태를 바꿨는가" — 동시 요청(1b)에서 진 쪽·이미 같은 상태(2a)는 false.
public interface IBookmarkRepository
{
    // erd.md bookmarks Q1. 이 장소를 북마크했는가(R-STAR 입력). 받는 인덱스: pk_bookmarks.
    Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct);

    // erd.md bookmarks Q2. INSERT ... ON CONFLICT (user_id, place_id) DO NOTHING.
    Task<bool> TryAddAsync(Bookmark bookmark, CancellationToken ct);

    // erd.md bookmarks Q2. DELETE ... WHERE user_id = ? AND place_id = ?
    Task<bool> RemoveAsync(UserId userId, PlaceId placeId, CancellationToken ct);

    // BM-03 내 지도. 북마크했거나 리뷰를 쓴 장소 전부(합집합, 장소당 1행)와 별 판정 입력 두 개. 쿼리 한 번.
    // 받는 인덱스: bookmarks Q3(pk_bookmarks 앞 열), reviews Q4(ux_reviews_user_place_date 앞 두 열), places PK — 예상, D8에서 EXPLAIN.
    // 상한 없음: 지도는 한 번에 다 그린다. 개수·bbox 필터는 D8 시드 데이터로 측정한 뒤 정한다(2026-10-25 사용자 결정).
    Task<IReadOnlyList<MyPlace>> ListMyPlacesAsync(UserId userId, CancellationToken ct);
}

public sealed record MyPlace(Place Place, bool IsBookmarked, bool HasReview);
