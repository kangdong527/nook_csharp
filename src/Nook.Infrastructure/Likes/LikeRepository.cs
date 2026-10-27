using Microsoft.EntityFrameworkCore;
using Nook.Application.Likes;
using Nook.Domain.Likes;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;
using Npgsql;

namespace Nook.Infrastructure.Likes;

internal sealed class LikeRepository(NookDbContext db) : ILikeRepository
{
    public async Task<AddLikeOutcome> TryAddAsync(Like like, CancellationToken ct)
    {
        // erd.md likes Q2. 충돌 판정: ux_likes_review_user(PF-02에서 PK → UNIQUE). 새 좋아요의 UserId는 항상 있다(Like.Create). ON CONFLICT는 UNIQUE·PK 충돌만 받는다 — FK 위반(23503)은 예외로 올라온다.
        try
        {
            var inserted = await db.Database.ExecuteSqlAsync($"""
                INSERT INTO likes (id, review_id, user_id, created_at)
                VALUES ({Guid.CreateVersion7()}, {like.ReviewId.Value}, {like.UserId!.Value}, {like.CreatedAt})
                ON CONFLICT (review_id, user_id) DO NOTHING
                """, ct);
            return inserted == 1 ? AddLikeOutcome.Added : AddLikeOutcome.AlreadyLiked;
        }
        // ExecuteSql은 SaveChanges를 거치지 않아 DbUpdateException으로 감싸지 않는다.
        catch (PostgresException ex) when (ex is
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "fk_likes_reviews",
        })
        {
            return AddLikeOutcome.ReviewGone;
        }
        // fk_likes_users(탈퇴한 회원의 남은 토큰)는 일부러 잡지 않는다 → 500. BM도 같다 — PF-02에서 함께 정한다.
    }

    public async Task<bool> RemoveAsync(UserId userId, ReviewId reviewId, CancellationToken ct) =>
        // erd.md likes Q2. 받는 인덱스: pk_likes.
        await db.Likes
            .Where(l => l.ReviewId == reviewId && l.UserId == userId)
            .ExecuteDeleteAsync(ct) == 1;
}
