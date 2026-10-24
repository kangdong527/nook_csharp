using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Application.Reviews;

// 리뷰 저장 담당(erd.md reviews).
public interface IReviewRepository
{
    // 이 회원이 이 장소에 리뷰를 쓴 적이 있는가(R-STAR 입력). 받는 인덱스: reviews (user_id, place_id, review_date) UNIQUE의 앞 두 열.
    Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct);

    // erd.md reviews Q1. LK-01 좋아요 대상 확인. 받는 인덱스: pk_reviews.
    Task<bool> ExistsAsync(ReviewId reviewId, CancellationToken ct);

    // RV-01 4단계 사전 확인(R-RV-03). erd.md reviews Q5의 UNIQUE 세 열 그대로. 업로드 전에 걸러 고아 이미지를 줄이는 용도 —
    // 최종 판정은 TryAddAsync의 UNIQUE다(동시 작성 6a).
    Task<bool> ExistsOnDateAsync(UserId userId, PlaceId placeId, DateOnly reviewDate, CancellationToken ct);

    // RV-01 6단계. 리뷰와 이미지를 한 번에 저장한다(SaveChanges 한 번 = 트랜잭션 하나).
    Task<AddReviewOutcome> TryAddAsync(Review review, CancellationToken ct);

    // RV-02 4단계. 수정할 리뷰를 이미지와 함께 추적 상태로 읽는다(Images는 소유 엔티티라 함께 온다). 없으면 null. 받는 인덱스: pk_reviews.
    Task<Review?> GetAsync(ReviewId reviewId, CancellationToken ct);

    // RV-02 6단계. GetAsync로 읽어 바꾼 리뷰를 저장한다(SaveChanges 한 번 = 트랜잭션 하나).
    Task<SaveReviewOutcome> TrySaveAsync(Review review, CancellationToken ct);

    // RV-03 2단계(ADR-009 결정 3). id와 user_id가 모두 맞는 리뷰를 한 문장으로 지운다. 0행이면 null — 이유(없음·남의 것)는 호출 쪽이 가린다.
    // 지운 리뷰의 이미지 키도 같은 문장에서 돌려준다(커밋 후 스토리지 삭제용). review_images·likes는 CASCADE. 받는 인덱스: pk_reviews.
    Task<ReviewDeletion?> DeleteAsync(ReviewId reviewId, UserId userId, CancellationToken ct);
}

public sealed record ReviewDeletion(PlaceId PlaceId, IReadOnlyList<string> StorageKeys);

public enum AddReviewOutcome
{
    Added,
    // ux_reviews_user_place_date 위반 — 사전 확인을 함께 통과한 동시 작성에서 진 쪽.
    AlreadyReviewedToday,
}

public enum SaveReviewOutcome
{
    Saved,
    // 6b. 읽은 뒤 저장 전에 RV-03이 리뷰를 지웠다 — UPDATE/DELETE 0행 또는 review_images INSERT의 FK 위반.
    ReviewGone,
    // 6c. 읽은 뒤 저장 전에 다른 RV-02가 같은 리뷰의 이미지를 바꿨다 — 리뷰는 남아 있다(2026-10-24 사용자 결정: 409로 구분).
    Conflict,
}
