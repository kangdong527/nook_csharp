using Nook.Domain.Common;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Domain.Likes;

// 좋아요 애그리거트 루트. 식별자는 (UserId, ReviewId) 복합 키다.
// R-LK-01(회원·리뷰당 0/1건)은 다른 인스턴스와 비교해야 하므로 DB가 최종 보장한다.
// Review에 넣지 않은 이유: 인기 리뷰 하나에 좋아요가 몰리면 Review 행 잠금 경합이 생긴다(분류표).
// 동등성 재정의 안 함: Like끼리 비교하는 코드가 없고, 같은 (UserId, ReviewId)의 중복은 DB PK가 막는다.
public sealed class Like
{
    public UserId UserId { get; }
    public ReviewId ReviewId { get; }
    public DateTimeOffset CreatedAt { get; }

    private Like(UserId userId, ReviewId reviewId, DateTimeOffset createdAt)
    {
        UserId = userId;
        ReviewId = reviewId;
        CreatedAt = createdAt;
    }

    public static Like Create(UserId userId, ReviewId reviewId, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(reviewId);

        return new Like(userId, reviewId, TimeGuard.Utc(createdAt));
    }
}
