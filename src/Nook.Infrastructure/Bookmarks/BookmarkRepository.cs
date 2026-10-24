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

    public async Task<bool> RemoveAsync(UserId userId, PlaceId placeId, CancellationToken ct) =>
        // erd.md bookmarks Q2. 받는 인덱스: pk_bookmarks. 동시 해제(1b)는 행 잠금으로 직렬화 — 진 쪽은 기다린 뒤 0행.
        await db.Set<Bookmark>()
            .Where(b => b.UserId == userId && b.PlaceId == placeId)
            .ExecuteDeleteAsync(ct) == 1;
}
