using Microsoft.EntityFrameworkCore;
using Nook.Application.Reviews;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;
using Npgsql;

namespace Nook.Infrastructure.Reviews;

internal sealed class ReviewRepository(NookDbContext db) : IReviewRepository
{
    public Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct) =>
        db.Reviews.AnyAsync(r => r.UserId == userId && r.PlaceId == placeId, ct);

    public Task<bool> ExistsAsync(ReviewId reviewId, CancellationToken ct) =>
        db.Reviews.AnyAsync(r => r.Id == reviewId, ct);

    public Task<bool> ExistsOnDateAsync(UserId userId, PlaceId placeId, DateOnly reviewDate, CancellationToken ct) =>
        // erd.md reviews Q5. 받는 인덱스: ux_reviews_user_place_date.
        db.Reviews.AnyAsync(r => r.UserId == userId && r.PlaceId == placeId && r.ReviewDate == reviewDate, ct);

    public async Task<AddReviewOutcome> TryAddAsync(Review review, CancellationToken ct)
    {
        // erd.md reviews Q5. 충돌 판정: ux_reviews_user_place_date. 진 쪽은 상대 커밋을 기다린 뒤 23505.
        // SaveChanges를 거치므로 PostgresException은 DbUpdateException 안에 감싸여 온다(ExecuteSql과 다름 — LikeRepository).
        db.Reviews.Add(review);
        try
        {
            await db.SaveChangesAsync(ct);
            return AddReviewOutcome.Added;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_reviews_user_place_date",
        })
        {
            // 실패한 Added 상태가 남으면 같은 DbContext의 다음 SaveChanges가 같은 INSERT를 다시 보낸다. 요청당 DbContext라 비워도 된다.
            db.ChangeTracker.Clear();
            return AddReviewOutcome.AlreadyReviewedToday;
        }
        // fk_reviews_places(장소 삭제)는 잡지 않는다 — 장소는 삭제하지 않는다는 불변식(progress.md ADR 권장 3)에 기댄다.
    }
}
