using Microsoft.EntityFrameworkCore;
using Nook.Domain.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Constraints;

// PF-02 스키마(PM: 탈퇴해도 리뷰·좋아요는 남는다). 탈퇴 절차를 거치지 않고 users 행만 지워 FK 동작 자체를 본다.
public sealed class WithdrawalSchemaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // 테스트 목록
    //
    // - DeleteUser_KeepsReviewAndLikes_WithNullUser       (fk_reviews_users·fk_likes_users SET NULL)
    // - LikesOfTwoWithdrawnUsers_CoexistOnOneReview       (ux_likes_review_user — NULL끼리는 다르다)

    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeleteUser_KeepsReviewAndLikes_WithNullUser()
    {
        var (author, liker) = (NewUser(1), NewUser(2));
        var place = NewPlace();
        var review = NewReview(author, place);
        await SaveAsync(author, liker, place, review, Like.Create(liker.Id, review.Id, Now), Like.Create(author.Id, review.Id, Now));

        await using (var db = Db.CreateContext())
            await db.Database.ExecuteSqlAsync($"DELETE FROM users WHERE id IN ({author.Id.Value}, {liker.Id.Value})");

        await using var check = Db.CreateContext();
        Assert.Null((await check.Reviews.SingleAsync()).UserId);
        Assert.Equal([null, null], await check.Likes.Select(l => l.UserId).ToListAsync());
    }

    [Fact]
    public async Task LikesOfTwoWithdrawnUsers_CoexistOnOneReview()
    {
        var (author, a, b) = (NewUser(1), NewUser(2), NewUser(3));
        var place = NewPlace();
        var review = NewReview(author, place);
        await SaveAsync(author, a, b, place, review, Like.Create(a.Id, review.Id, Now), Like.Create(b.Id, review.Id, Now));

        await using (var db = Db.CreateContext())
            await db.Database.ExecuteSqlAsync($"DELETE FROM users WHERE id IN ({a.Id.Value}, {b.Id.Value})");

        await using var check = Db.CreateContext();
        Assert.Equal(2, await check.Likes.CountAsync(l => l.ReviewId == review.Id));   // 좋아요 수가 줄지 않는다
    }

    private static User NewUser(int n) => User.Create(Nickname.From($"회원{n}"), Email.From($"u{n}@example.com"), Gender.None, null);

    private static Place NewPlace() =>
        Place.Create(KakaoPlaceId.From("1"), "누크 카페", "서울", Coordinate.Create(37.5, 127.0).Unwrap()).Unwrap();

    private static Review NewReview(User author, Place place) =>
        Review.Create(author.Id, place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), [], Now).Unwrap();

    private async Task SaveAsync(params object[] entities)
    {
        await using var db = Db.CreateContext();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }
}
