using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Users;

namespace Nook.Application.Bookmarks;

// 북마크 저장 담당(erd.md bookmarks). R-BM-01(회원·장소당 0/1건)은 pk_bookmarks가 최종 보장한다.
// 반환값은 "이번 요청이 상태를 바꿨는가" — 동시 요청(1b)에서 진 쪽·이미 같은 상태(2a)는 false.
public interface IBookmarkRepository
{
    // erd.md bookmarks Q2. INSERT ... ON CONFLICT (user_id, place_id) DO NOTHING.
    Task<bool> TryAddAsync(Bookmark bookmark, CancellationToken ct);

    // erd.md bookmarks Q2. DELETE ... WHERE user_id = ? AND place_id = ?
    Task<bool> RemoveAsync(UserId userId, PlaceId placeId, CancellationToken ct);
}
