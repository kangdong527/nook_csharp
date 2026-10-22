using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Application.Reviews;

// 리뷰 저장 담당(erd.md reviews). RV-01에서 쓰기 메서드가 추가된다.
public interface IReviewRepository
{
    // 이 회원이 이 장소에 리뷰를 쓴 적이 있는가(R-STAR 입력). 받는 인덱스: reviews (user_id, place_id, review_date) UNIQUE의 앞 두 열.
    Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct);

    // erd.md reviews Q1. LK-01 좋아요 대상 확인. 받는 인덱스: pk_reviews.
    Task<bool> ExistsAsync(ReviewId reviewId, CancellationToken ct);
}
