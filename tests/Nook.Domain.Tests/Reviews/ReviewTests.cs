using Nook.SharedKernel;
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
    //
    // Create — 시각 가드 (ADR-007)
    // - Create_WhenCreatedAtIsInvalid_ThrowsWithParamName   (default, 비UTC +09:00)
    // - Create_WhenThreeImages_KeepsOrder                   (이미지 상한: 3장 성공, SortOrder 0·1·2)
    // - Create_WhenTooManyImages_ReturnsTooManyImages       (상한 + 1: 4장. 아래 테스트의 기준선이기도 하다)
    // - Create_WhenCreatedAtIsDefaultAndTooManyImages_Throws (예외 먼저 — 버그가 검증 실패로 위장되지 않는다)

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

    // DateTimeOffset은 attribute 인자(InlineData)가 될 수 없어 TheoryData로 넘긴다.
    public static TheoryData<DateTimeOffset> InvalidCreatedAt =>
    [
        default,                                                        // 호출부가 시각을 빠뜨림
        new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.FromHours(9)),   // DateTimeOffset.Now 직접 호출
    ];

    [Theory]
    [MemberData(nameof(InvalidCreatedAt))]
    public void Create_WhenCreatedAtIsInvalid_ThrowsWithParamName(DateTimeOffset createdAt)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Review.Create(UserId.New(), PlaceId.New(), ValidContent(), [ValidKey(0)], createdAt));

        // 결과를 utcCreatedAt 변수에 담아도 ParamName은 가드에 넘긴 인자 식(createdAt)이다.
        Assert.Equal("createdAt", ex.ParamName);
    }

    [Fact]
    public void Create_WhenThreeImages_KeepsOrder()
    {
        string[] keys = [ValidKey(0), ValidKey(1), ValidKey(2)];

        var result = Review.Create(UserId.New(), PlaceId.New(), ValidContent(), keys, Now);

        Assert.True(result.TryGetValue(out var review, out var error), error?.Code);
        Assert.Equal(keys, review.Images.Select(i => i.StorageKey));
        Assert.Equal([0, 1, 2], review.Images.Select(i => i.SortOrder));
    }

    [Fact]
    public void Create_WhenTooManyImages_ReturnsTooManyImages()
    {
        var result = Review.Create(UserId.New(), PlaceId.New(), ValidContent(), FourKeys(), Now);

        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(ReviewErrors.TooManyImages.Code, error?.Code);
    }

    [Fact]
    public void Create_WhenCreatedAtIsDefaultAndTooManyImages_Throws()
    {
        // 위 기준선과 시각만 다르다. TooManyImages가 돌아오면 호출 코드의 버그가 회원 실패로 위장된 것.
        var ex = Assert.Throws<ArgumentException>(
            () => Review.Create(UserId.New(), PlaceId.New(), ValidContent(), FourKeys(), default));

        Assert.Equal("createdAt", ex.ParamName);
    }

    private static string ValidKey(int n) => $"{KeyPrefix}{KeyId[..^1]}{n}.jpg";

    private static string[] FourKeys() => [ValidKey(0), ValidKey(1), ValidKey(2), ValidKey(3)];

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
