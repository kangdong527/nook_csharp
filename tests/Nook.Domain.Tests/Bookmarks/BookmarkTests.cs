using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Users;

namespace Nook.Domain.Tests.Bookmarks;

// Bookmark 단위 테스트. 시각 가드(ADR-007)를 엔티티가 실제로 호출하는지 확인한다 —
// 가드를 빠뜨려도 컴파일되므로 TimeGuard 단독 테스트로는 잡을 수 없다.
public class BookmarkTests
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
            () => Bookmark.Create(UserId.New(), PlaceId.New(), createdAt));

        // [CallerArgumentExpression]이 엔티티 안의 인자 식을 담는다 → 어느 엔티티의 어느 시각인지 드러난다.
        Assert.Equal("createdAt", ex.ParamName);
    }
}
