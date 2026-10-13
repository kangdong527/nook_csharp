using Nook.Domain.Common;
using Nook.Domain.Places;
using Nook.Domain.Users;

namespace Nook.Domain.Bookmarks;

// 북마크 애그리거트 루트. 식별자는 (UserId, PlaceId) 복합 키다.
// R-BM-01(회원·장소당 0/1건)·R-BM-02(저장·해제 경합)는 다른 인스턴스와 비교해야 하므로 DB가 최종 보장한다.
// 생성 시 검사할 도메인 규칙이 없어 Result 없이 Bookmark를 바로 반환한다.
// 동등성 재정의 안 함: Bookmark끼리 비교하는 코드가 없고, 같은 (UserId, PlaceId)의 중복은 DB PK가 막는다.
public sealed class Bookmark
{
    public UserId UserId { get; }
    public PlaceId PlaceId { get; }
    public DateTimeOffset CreatedAt { get; }

    private Bookmark(UserId userId, PlaceId placeId, DateTimeOffset createdAt)
    {
        UserId = userId;
        PlaceId = placeId;
        CreatedAt = createdAt;
    }

    public static Bookmark Create(UserId userId, PlaceId placeId, DateTimeOffset createdAt)
    {
        // null·default·비UTC는 사용자 입력이 아니라 호출 코드의 실수이므로 예외로 알린다.
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(placeId);

        return new Bookmark(userId, placeId, TimeGuard.Utc(createdAt));
    }
}
