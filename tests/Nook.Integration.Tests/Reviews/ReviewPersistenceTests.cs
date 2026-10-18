using Microsoft.EntityFrameworkCore;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Reviews;

// reviews + review_images 저장·복원 통합 테스트. 실제 PostgreSQL(마이그레이션 적용 스키마)에서 확인한다.
public sealed class ReviewPersistenceTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // 테스트 목록
    //
    // 이미지 순서 — ux_review_images_review_sort_order(DEFERRABLE INITIALLY DEFERRED, 마이그레이션 SQL)
    // - SaveChanges_WhenSortOrdersSwapped_ReloadsInNewOrder   (0↔1, D3-3에서는 SQL로만 확인)

    // timestamptz는 마이크로초까지만 저장한다(GroupPersistenceTests와 같은 이유).
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_WhenSortOrdersSwapped_ReloadsInNewOrder()
    {
        // Arrange — 리뷰는 users·places FK가 있으므로 둘 다 함께 저장한다.
        var user = User.Create(Nickname.From("누크"), Email.From("nook@example.com"));
        var place = Place.Create(
            KakaoPlaceId.From("12345"), "누크 카페", "서울 중구", Coordinate.Create(37.5665, 126.9780).Unwrap()).Unwrap();
        var review = Review.Create(
            user.Id, place.Id, ReviewContent.From("조용하고 커피가 맛있는 곳"), ["img/a.jpg", "img/b.jpg"], CreatedAt).Unwrap();

        await using (var db = Db.CreateContext())
        {
            db.AddRange(user, place, review);
            await db.SaveChangesAsync();
        }

        // Act — 저장된 상태에서 시작해야 INSERT가 아니라 UPDATE 두 개가 나간다 → 새 컨텍스트로 읽어 온다.
        // 순서 변경 공개 메서드는 아직 없다(RV-02, D5). 도메인을 거치지 않고 변경 추적기에서 SortOrder만 바꾼다.
        // Images 게터는 목록만 복사하고 요소는 추적 중인 같은 인스턴스이므로 Entry로 접근할 수 있다.
        await using (var db = Db.CreateContext())
        {
            var loaded = await db.Reviews.SingleAsync(r => r.Id == review.Id);
            var images = loaded.Images;   // 게터가 매번 새 목록을 만드므로 한 번만 읽는다

            db.Entry(images[0]).Property(i => i.SortOrder).CurrentValue = 1;
            db.Entry(images[1]).Property(i => i.SortOrder).CurrentValue = 0;

            // UPDATE 두 개가 한 트랜잭션에서 차례로 실행된다. 첫 UPDATE 직후 (review_id, 1)이 두 행 —
            // 제약이 즉시 검사였다면 여기서 23505. DEFERRED라 커밋 시점에 한 번만 검사한다.
            await db.SaveChangesAsync();
        }

        // Assert — 검증용 컨텍스트. 키 b가 앞으로 오고, 각 위치의 SortOrder가 인덱스와 같다.
        await using var verify = Db.CreateContext();
        var reloaded = (await verify.Reviews.SingleAsync(r => r.Id == review.Id)).Images;

        Assert.Equal(["img/b.jpg", "img/a.jpg"], reloaded.Select(i => i.StorageKey));
        Assert.All(reloaded, (image, index) => Assert.Equal(index, image.SortOrder));
    }
}
