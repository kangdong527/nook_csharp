using Nook.Domain.Likes;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Domain.Tests.Likes;

// Like 단위 테스트. 시각 가드(ADR-007)를 엔티티가 실제로 호출하는지 확인한다(BookmarkTests와 같은 형식).
public class LikeTests
{
    // 테스트 목록
    //
    // Create (ADR-007)
    // - Create_WhenCreatedAtIsInvalid_ThrowsWithParamName   (default, 비UTC +09:00)

    // DateTimeOffset은 attribute 인자(InlineData)가 될 수 없어 TheoryData로 넘긴다.
    public static TheoryData<DateTimeOffset> InvalidCreatedAt =>
    [
        default,                                                        // 호출부가 시각을 빠뜨림
        new DateTimeOffset(2026, 10, 18, 9, 0, 0, TimeSpan.FromHours(9)),   // DateTimeOffset.Now 직접 호출
    ];

    [Theory]
    [MemberData(nameof(InvalidCreatedAt))]
    public void Create_WhenCreatedAtIsInvalid_ThrowsWithParamName(DateTimeOffset createdAt)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Like.Create(UserId.New(), ReviewId.New(), createdAt));

        Assert.Equal("createdAt", ex.ParamName);
    }
}
