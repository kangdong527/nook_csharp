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

    public Task<Review?> GetAsync(ReviewId reviewId, CancellationToken ct) =>
        // erd.md reviews Q1. 추적 쿼리 — 핸들러가 Review.Edit으로 바꾼 것을 TrySaveAsync가 그대로 저장한다.
        db.Reviews.SingleOrDefaultAsync(r => r.Id == reviewId, ct);

    public async Task<SaveReviewOutcome> TrySaveAsync(Review review, CancellationToken ct)
    {
        // 4단계 조회 뒤 다른 요청이 커밋했으면, 무엇을 바꿨느냐에 따라 다른 모양으로 드러난다:
        //   reviews UPDATE(version 토큰)·이미지 DELETE/순서 UPDATE → 0행 → DbUpdateConcurrencyException — 리뷰 삭제(6b)일 수도,
        //     다른 RV-02가 먼저 커밋한 것(6c, version이 바뀜)일 수도 있어 리뷰가 남아 있는지 다시 본다.
        //     Edit는 항상 Version을 올리므로 reviews UPDATE가 반드시 나간다 — 바뀌지 않은 이미지는 문장이 없어 0행만으로는 6c를 놓쳤다(incident 002)
        //   새 이미지 INSERT → fk_review_images_reviews(23503) → 리뷰 삭제(6b)
        //   새 이미지 sort_order가 다른 RV-02와 겹침 → ux_review_images_review_sort_order(23505, DEFERRED라 커밋 시점) → 6c
        //
        // 리뷰 행을 먼저 잠근다(incident 002). 잠그지 않으면 두 RV-02가 같은 sort_order를 INSERT할 때, 나중에 INSERT한 쪽의
        // DEFERRED 재검사(COMMIT)는 상대 트랜잭션을 기다리고 상대는 이쪽이 쥔 reviews 행(본문 UPDATE)을 기다려 데드락(40P01)이 난다.
        // 잠그면 같은 리뷰 수정은 처음부터 한 줄로 서고, 진 쪽은 위의 0행·23505 경로로 Conflict가 된다. 리뷰가 없으면 잠글 행이 없어 그대로 진행 → 0행 → 6b.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlAsync($"SELECT 1 FROM reviews WHERE id = {review.Id.Value} FOR UPDATE", ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return SaveReviewOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            // 실패한 문장 뒤 트랜잭션은 aborted 상태라 아래 재조회를 하려면 먼저 되돌려야 한다.
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return await db.Reviews.AnyAsync(r => r.Id == review.Id, ct)
                ? SaveReviewOutcome.Conflict
                : SaveReviewOutcome.ReviewGone;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "fk_review_images_reviews",
        })
        {
            db.ChangeTracker.Clear();
            return SaveReviewOutcome.ReviewGone;
        }
        catch (Exception ex) when (IsSortOrderViolation(ex))
        {
            db.ChangeTracker.Clear();
            return SaveReviewOutcome.Conflict;
        }
    }

    // DEFERRED 제약은 문장 실행이 아니라 COMMIT에서 실패한다. 그 경우 EF가 DbUpdateException으로 감싸지 않을 수 있어 두 모양을 모두 본다.
    private static bool IsSortOrderViolation(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException) is
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_review_images_review_sort_order",
        };
}
