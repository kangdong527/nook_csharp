using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Domain.Tests.Reviews;

// Review.Edit(RV-02) 단위 테스트.
// 계약: 최종 순서 = keepImageIds 순서 → newStorageKeys 순서, sortOrder = 최종 목록 인덱스. 반환 = 빠진 이미지의 키.
// 실패하면 Content·Images 모두 그대로여야 한다(검사 먼저, 변경은 나중).
public class ReviewEditTests
{
    // 테스트 목록
    //
    // 성공
    // - Edit_ReorderKept_FollowsKeepOrder                      ([A, B] → keep [B, A])
    // - Edit_KeepOneAndAddTwo_AppendsNewAfterKeptAndReturnsRemoved ([A, B, C] → keep [C] + new [X, Y])
    // - Edit_EachSuccess_IncrementsVersion                     (동시성 토큰, 실패하면 그대로 — Snapshot에 포함)
    //
    // 실패 — 애그리거트는 그대로
    // - Edit_WhenTotalExceedsMax_ReturnsTooManyImages          (keep 2 + new 2 = 4, 3c)
    // - Edit_WhenKeepIdIsDuplicated_ReturnsImageNotInReview    ([A, A], 4c)
    // - Edit_WhenKeepIdIsFromOtherReview_ReturnsImageNotInReview (4c)
    // - Edit_WhenNewKeyIsInvalid_ThrowsAndLeavesReviewUnchanged (57자 키 — 생성 코드 버그)

    private static readonly DateTimeOffset Now = new(2026, 10, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly ReviewContent Before = ReviewContent.From("수정 전 본문입니다 열 자");
    private static readonly ReviewContent After = ReviewContent.From("수정 후 본문입니다 열 자");

    [Fact]
    public void Edit_ReorderKept_FollowsKeepOrder()
    {
        var review = CreateReview("a.jpg", "b.jpg");
        var (a, b) = (review.Images[0], review.Images[1]);

        var removed = Ok(review.Edit(After, [b.Id, a.Id], []));

        Assert.Empty(removed);
        Assert.Equal(["b.jpg", "a.jpg"], Keys(review));
        // 같은 이미지는 Id를 유지한다 — 순서 변경은 교체가 아니다.
        Assert.Equal([b.Id, a.Id], review.Images.Select(i => i.Id));
    }

    [Fact]
    public void Edit_KeepOneAndAddTwo_AppendsNewAfterKeptAndReturnsRemoved()
    {
        var review = CreateReview("a.jpg", "b.jpg", "c.jpg");
        var c = review.Images[2];

        var removed = Ok(review.Edit(After, [c.Id], ["x.jpg", "y.jpg"]));

        Assert.Equal(["a.jpg", "b.jpg"], removed.Order());
        Assert.Equal(["c.jpg", "x.jpg", "y.jpg"], Keys(review));
        Assert.Equal([0, 1, 2], review.Images.Select(i => i.SortOrder));
        Assert.Equal(After, review.Content);
    }

    [Fact]
    public void Edit_EachSuccess_IncrementsVersion()
    {
        // 동시성 토큰(incident 002): 무엇을 바꿨든 성공한 수정마다 오른다 — 순서만 바꾼 수정도 다른 수정과 충돌해야 한다.
        var review = CreateReview("a.jpg", "b.jpg");
        Assert.Equal(0, review.Version);

        Ok(review.Edit(Before, [.. review.Images.Select(i => i.Id).Reverse()], []));
        Ok(review.Edit(Before, [.. review.Images.Select(i => i.Id)], []));

        Assert.Equal(2, review.Version);
    }

    [Fact]
    public void Edit_WhenTotalExceedsMax_ReturnsTooManyImages()
    {
        var review = CreateReview("a.jpg", "b.jpg");
        var snapshot = Snapshot(review);

        var result = review.Edit(After, [.. review.Images.Select(i => i.Id)], ["x.jpg", "y.jpg"]);

        AssertFailedWith(ReviewErrors.TooManyImages, result);
        Assert.Equal(snapshot, Snapshot(review));
    }

    [Fact]
    public void Edit_WhenKeepIdIsDuplicated_ReturnsImageNotInReview()
    {
        var review = CreateReview("a.jpg", "b.jpg", "c.jpg");
        var a = review.Images[0];
        var snapshot = Snapshot(review);

        var result = review.Edit(After, [a.Id, a.Id], ["x.jpg"]);

        AssertFailedWith(ReviewErrors.ImageNotInReview, result);
        Assert.Equal(snapshot, Snapshot(review));
    }

    [Fact]
    public void Edit_WhenKeepIdIsFromOtherReview_ReturnsImageNotInReview()
    {
        var review = CreateReview("a.jpg");
        var other = CreateReview("z.jpg").Images[0];
        var snapshot = Snapshot(review);

        var result = review.Edit(After, [other.Id], []);

        AssertFailedWith(ReviewErrors.ImageNotInReview, result);
        Assert.Equal(snapshot, Snapshot(review));
    }

    [Fact]
    public void Edit_WhenNewKeyIsInvalid_ThrowsAndLeavesReviewUnchanged()
    {
        var review = CreateReview("a.jpg", "b.jpg");
        var a = review.Images[0];
        var snapshot = Snapshot(review);
        var tooLong = new string('k', ReviewImage.StorageKeyMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => review.Edit(After, [a.Id], [tooLong]));
        Assert.Equal(snapshot, Snapshot(review));
    }

    private static Review CreateReview(params string[] keys) =>
        Ok(Review.Create(UserId.New(), PlaceId.New(), Before, keys, Now));

    private static string[] Keys(Review review) => [.. review.Images.Select(i => i.StorageKey)];

    // 실패 후 "그대로"의 기준: 본문, 버전, 이미지 (Id, 키, 순서).
    private static (ReviewContent, int, string) Snapshot(Review review) =>
        (review.Content, review.Version, string.Join("|", review.Images.Select(i => $"{i.Id.Value}:{i.StorageKey}:{i.SortOrder}")));

    private static T Ok<T>(Result<T> result)
    {
        Assert.True(result.TryGetValue(out var value, out var error), error?.Code);
        return value;
    }

    private static void AssertFailedWith(Error expected, Result<IReadOnlyList<string>> result)
    {
        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(expected, error);
    }
}
