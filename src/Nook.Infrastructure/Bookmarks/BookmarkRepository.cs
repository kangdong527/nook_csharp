using Microsoft.EntityFrameworkCore;
using Nook.Application.Bookmarks;
using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;

namespace Nook.Infrastructure.Bookmarks;

internal sealed class BookmarkRepository(NookDbContext db) : IBookmarkRepository
{
    public Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct) =>
        db.Set<Bookmark>().AnyAsync(b => b.UserId == userId && b.PlaceId == placeId, ct);

    public async Task<bool> TryAddAsync(Bookmark bookmark, CancellationToken ct)
    {
        // erd.md bookmarks Q2. 충돌 판정: pk_bookmarks. 동시 저장(1b)은 PK가 직렬화한다 — 진 쪽은 상대 커밋을 기다린 뒤 0행.
        var inserted = await db.Database.ExecuteSqlAsync($"""
            INSERT INTO bookmarks (user_id, place_id, created_at)
            VALUES ({bookmark.UserId.Value}, {bookmark.PlaceId.Value}, {bookmark.CreatedAt})
            ON CONFLICT (user_id, place_id) DO NOTHING
            """, ct);
        return inserted == 1;
    }

    public async Task<IReadOnlyList<MyPlace>> ListMyPlacesAsync(UserId userId, CancellationToken ct)
    {
        // 원본 nook은 북마크 장소·리뷰 장소를 따로 읽어 앱에서 Map으로 합쳤다(앱 측 조인, 왕복 2번, 중복 장소 전송). 여기선 DB가 합친다.
        // 회원의 행에서 출발한다: places 전체를 훑으며 EXISTS OR EXISTS를 묻는 대신, 이 회원의 place_id 합집합을 먼저 만들고
        // 그 장소만 PK로 읽는다. 장소가 늘어도 비용은 "이 회원의 기록 수"를 따라간다(예상 — D8에서 EXPLAIN).
        // 장소당 1행은 IN(세미 조인)이 보장한다 — 하위 쿼리에 같은 place_id가 여러 번 있어도 places 행은 한 번만 나온다.
        // UNION(중복 제거)은 하위 쿼리 결과를 줄일 뿐 정확성에는 필요 없다(Concat으로 바꿔도 테스트 통과, 2026-10-25 확인).
        var placeIds = db.Bookmarks.Where(b => b.UserId == userId).Select(b => b.PlaceId)
            .Union(db.Reviews.Where(r => r.UserId == userId).Select(r => r.PlaceId));

        return await db.Places
            .AsNoTracking()
            .Where(p => placeIds.Contains(p.Id))
            // 지도에는 순서가 의미 없지만, 같은 요청에 같은 응답을 주도록 고정한다(UUID v7 → 장소 등록 순).
            .OrderBy(p => p.Id)
            .Select(p => new MyPlace(
                p,
                db.Bookmarks.Any(b => b.UserId == userId && b.PlaceId == p.Id),
                db.Reviews.Any(r => r.UserId == userId && r.PlaceId == p.Id)))
            .ToListAsync(ct);
    }

    public async Task<bool> RemoveAsync(UserId userId, PlaceId placeId, CancellationToken ct) =>
        // erd.md bookmarks Q2. 받는 인덱스: pk_bookmarks. 동시 해제(1b)는 행 잠금으로 직렬화 — 진 쪽은 기다린 뒤 0행.
        await db.Set<Bookmark>()
            .Where(b => b.UserId == userId && b.PlaceId == placeId)
            .ExecuteDeleteAsync(ct) == 1;
}
