using Nook.Domain.Likes;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Application.Likes;

// INSERT 결과. bool로는 "이미 있음"과 "리뷰가 사라짐"을 가를 수 없다.
public enum AddLikeOutcome
{
    Added,
    AlreadyLiked,
    // 사전 확인 뒤 INSERT 전에 리뷰가 삭제됨(fk_likes_reviews 위반). 장소와 달리 리뷰는 삭제 유스케이스가 있다.
    ReviewGone,
}

// 좋아요 저장 담당(erd.md likes). R-LK-01(회원·리뷰당 0/1건)은 pk_likes가 최종 보장한다.
public interface ILikeRepository
{
    // erd.md likes Q2. INSERT ... ON CONFLICT (review_id, user_id) DO NOTHING.
    Task<AddLikeOutcome> TryAddAsync(Like like, CancellationToken ct);

    // erd.md likes Q2. DELETE ... WHERE review_id = ? AND user_id = ? — 지웠으면 true.
    Task<bool> RemoveAsync(UserId userId, ReviewId reviewId, CancellationToken ct);
}
