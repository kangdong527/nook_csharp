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

    public async Task<ReviewDeletion?> DeleteAsync(ReviewId reviewId, UserId userId, CancellationToken ct)
    {
        // 데이터 변경 CTE의 하위 문장은 모두 같은 스냅샷을 본다 → 바깥 SELECT에는 CASCADE로 지워지기 전의 review_images가 보인다.
        // 그래서 "지운 리뷰"와 "그 이미지 키"를 한 문장(= 한 판정)으로 얻는다. 키를 먼저 SELECT하고 DELETE하면 그 사이 RV-02가 끼어든다.
        // 데이터 변경 WITH는 최상위에만 올 수 있어, EF가 서브쿼리로 감싸는 연산자(FirstOrDefault 등) 대신 목록으로 받는다.
        var rows = await db.Database.SqlQuery<DeletedReviewRow>($"""
            WITH deleted AS (
                DELETE FROM reviews WHERE id = {reviewId.Value} AND user_id = {userId.Value}
                RETURNING id, place_id
            )
            SELECT d.place_id AS "PlaceId",
                   ARRAY(SELECT i.storage_key FROM review_images i WHERE i.review_id = d.id) AS "StorageKeys"
            FROM deleted d
            """).ToListAsync(ct);

        return rows switch
        {
            [] => null,
            [var row] => new ReviewDeletion(PlaceId.From(row.PlaceId), row.StorageKeys),
            _ => throw new InvalidOperationException($"PK로 지웠는데 {rows.Count}행이 지워졌다: {reviewId.Value}"),
        };
    }

    public async Task<IReadOnlyList<PlaceReview>> ListByPlaceAsync(
        PlaceId placeId, UserId me, ReviewCursor? after, int take, CancellationToken ct)
    {
        // 키셋 조건은 행 값 비교 (created_at, id) < (@c, @id) — 정렬 키와 같은 모양이라 "이 위치 다음부터"가 정확하다.
        // ReviewId에는 비교 연산자가 없어 LINQ로 쓸 수 없으므로 이 조건만 SQL로 두고, 정렬·개수·투영은 EF가 그 위에 합성한다.
        // 받는 인덱스(예상): ix_reviews_place_created (place_id, created_at DESC). id 동점 깨기는 인덱스 밖 — D8에서 EXPLAIN.
        var source = after is null
            ? db.Reviews.Where(r => r.PlaceId == placeId)
            : db.Reviews.FromSql($"""
                SELECT * FROM reviews
                WHERE place_id = {placeId.Value} AND (created_at, id) < ({after.CreatedAt}, {after.Id})
                """);

        var rows = await source
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .Select(r => new
            {
                Review = r,
                Nickname = db.Users.Where(u => u.Id == r.UserId).Select(u => u.Nickname).Single(),
                AvatarKey = db.Users.Where(u => u.Id == r.UserId).Select(u => u.AvatarKey).Single(),
                LikeCount = db.Likes.Count(l => l.ReviewId == r.Id),
                LikedByMe = db.Likes.Any(l => l.ReviewId == r.Id && l.UserId == me),
            })
            .ToListAsync(ct);

        return [.. rows.Select(row => new PlaceReview(row.Review, row.Nickname.Value, row.AvatarKey, row.LikeCount, row.LikedByMe))];
    }

    public async Task<IReadOnlyList<MyReview>> ListByUserAsync(
        UserId userId, ReviewCursor? after, int take, CancellationToken ct)
    {
        // ListByPlaceAsync와 같은 키셋. 받는 인덱스(예상): ux_reviews_user_place_date의 앞 컬럼 user_id로 범위를 좁힌 뒤
        // created_at 정렬은 인덱스 밖(한 회원의 리뷰 수만큼). 정렬 인덱스는 erd.md에서 보류한 것 — D8에서 EXPLAIN 후 결정.
        var source = after is null
            ? db.Reviews.Where(r => r.UserId == userId)
            : db.Reviews.FromSql($"""
                SELECT * FROM reviews
                WHERE user_id = {userId.Value} AND (created_at, id) < ({after.CreatedAt}, {after.Id})
                """);

        var rows = await source
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .Select(r => new
            {
                Review = r,
                PlaceName = db.Places.Where(p => p.Id == r.PlaceId).Select(p => p.Name).Single(),
                LikeCount = db.Likes.Count(l => l.ReviewId == r.Id),
                LikedByMe = db.Likes.Any(l => l.ReviewId == r.Id && l.UserId == userId),
            })
            .ToListAsync(ct);

        return [.. rows.Select(row => new MyReview(row.Review, row.PlaceName, row.LikeCount, row.LikedByMe))];
    }

    private sealed class DeletedReviewRow
    {
        public Guid PlaceId { get; init; }
        public string[] StorageKeys { get; init; } = [];
    }

    // DEFERRED 제약은 문장 실행이 아니라 COMMIT에서 실패한다. 그 경우 EF가 DbUpdateException으로 감싸지 않을 수 있어 두 모양을 모두 본다.
    private static bool IsSortOrderViolation(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException) is
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_review_images_review_sort_order",
        };
}
