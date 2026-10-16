using Nook.Domain.Common;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Domain.Tests.Reviews;

// Review 애그리거트 루트 단위 테스트.
// 규칙: 스토리지 키 상한(ReviewImage.StorageKeyMaxLength = 56)
//   키 규칙 reviews/{yyyy}/{MM}/{dd}/{32자 hex}.{jpg|png|webp} → 최대 56자(webp)
//   키는 회원 입력이 아니라 서버가 만드는 값이므로, 상한 초과는 키 생성 코드의 버그 → Result가 아니라 예외
public class ReviewTests
{
    // 테스트 목록
    //
    // Create — 스토리지 키 길이 (StorageKeyMaxLength = 56)
    // - Create_WhenStorageKeyIs55Chars_Succeeds      (확장자 3자: jpg·png)
    // - Create_WhenStorageKeyIs56Chars_Succeeds      (확장자 4자: webp, 상한 경계)
    // - Create_WhenStorageKeyIs57Chars_Throws        (상한 + 1, 생성 코드 버그 흉내)

    private static readonly DateTimeOffset Now = new(2026, 10, 16, 0, 0, 0, TimeSpan.Zero);

    private const string KeyPrefix = "reviews/2026/10/16/";              // 19자
    private const string KeyId = "3f2a9c0e7b1d4e8fa6c5d2b1e0f9a8c7";   // 32자

    [Fact]
    public void Create_WhenStorageKeyIs55Chars_Succeeds()
    {
        var key = $"{KeyPrefix}{KeyId}.jpg";
        Assert.Equal(55, key.Length);   // 전제 확인: 경계값이 의도대로 만들어졌는가

        var result = CreateWithKey(key);

        Assert.True(result.TryGetValue(out var review, out var error), error?.Code);
        Assert.Equal(key, Assert.Single(review.Images).StorageKey);
    }

    [Fact]
    public void Create_WhenStorageKeyIs56Chars_Succeeds()
    {
        var key = $"{KeyPrefix}{KeyId}.webp";
        Assert.Equal(56, key.Length);

        var result = CreateWithKey(key);

        Assert.True(result.TryGetValue(out var review, out var error), error?.Code);
        Assert.Equal(key, Assert.Single(review.Images).StorageKey);
    }

    [Fact]
    public void Create_WhenStorageKeyIs57Chars_Throws()
    {
        var key = $"{KeyPrefix}{KeyId}0.webp";   // 식별자가 33자가 된 생성 코드 버그
        Assert.Equal(57, key.Length);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateWithKey(key));

        Assert.Equal("storageKey", ex.ParamName);   // sortOrder 검사가 아니라 길이 검사가 던졌는가
    }

    private static Result<Review> CreateWithKey(string storageKey) =>
        Review.Create(UserId.New(), PlaceId.New(), ValidContent(), [storageKey], Now);

    private static ReviewContent ValidContent()
    {
        Assert.True(
            ReviewContent.Create("열 글자가 넘는 리뷰 본문").TryGetValue(out var content, out var error),
            error?.Code);
        return content;
    }
}
