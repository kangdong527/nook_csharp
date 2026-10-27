using Nook.Domain.Common;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Domain.Likes;

// 좋아요 애그리거트 루트. (UserId, ReviewId)가 회원·리뷰당 0/1건(R-LK-01) — DB에선 UNIQUE, PK는 대리 키(PF-02, 아래).
// UserId null = 탈퇴한 회원의 좋아요(PM: 좋아요는 지우지 않는다 — 리뷰의 좋아요 수가 줄지 않게). 복합 PK였다면 NULL이 될 수 없어
// 대리 키 id(EF 섀도 속성, 도메인은 모른다 — 좋아요를 ID로 가리키는 곳이 없다)로 바꿨다.
// R-LK-01(회원·리뷰당 0/1건)은 다른 인스턴스와 비교해야 하므로 DB가 최종 보장한다.
// Review에 넣지 않은 이유: 인기 리뷰 하나에 좋아요가 몰리면 Review 행 잠금 경합이 생긴다(분류표).
// 동등성 재정의 안 함: Like끼리 비교하는 코드가 없고, 같은 (UserId, ReviewId)의 중복은 DB PK가 막는다.
public sealed class Like
{
    public UserId? UserId { get; }
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
