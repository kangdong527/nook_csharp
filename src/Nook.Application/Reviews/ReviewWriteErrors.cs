using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// 리뷰 쓰기 유스케이스의 실패 중 Domain(ReviewErrors)이 판단할 수 없는 것 — 다른 애그리거트·DB·업로드 입력을 봐야 안다.
public static class ReviewWriteErrors
{
    // RV-01 3a.
    public static readonly Error PlaceNotFound = new(
        "Review.PlaceNotFound",
        "리뷰를 쓸 장소를 찾을 수 없습니다.",
        ErrorType.NotFound);

    // RV-01 4a·6a. 사전 확인에서 걸리든 UNIQUE 위반으로 걸리든 회원에게는 같은 상황.
    public static readonly Error AlreadyReviewedToday = new(
        "Review.AlreadyReviewedToday",
        "같은 장소에는 하루에 리뷰 1개만 작성할 수 있습니다.",
        ErrorType.Conflict);

    // RV-01 2c.
    public static readonly Error ImageTooLarge = new(
        "Review.ImageTooLarge",
        $"리뷰 이미지는 장당 {ReviewImageFile.MaxBytes / (1024 * 1024)}MB 이하여야 합니다.",
        ErrorType.Validation);

    public static readonly Error UnsupportedImageFormat = new(
        "Review.UnsupportedImageFormat",
        "리뷰 이미지는 JPG, PNG, WEBP만 올릴 수 있습니다.",
        ErrorType.Validation);

    // RV-02 4a·6b. 없는 ID이거나, 읽은 뒤 저장 전에 삭제됐다 — 회원에게는 같은 상황.
    public static readonly Error ReviewNotFound = new(
        "Review.NotFound",
        "리뷰를 찾을 수 없습니다.",
        ErrorType.NotFound);

    // RV-02 6c. 404로 뭉뚱그리면 리뷰가 있는데 없다고 안내하게 된다 — 다시 불러와 수정하면 되는 상황임을 알린다.
    public static readonly Error EditConflict = new(
        "Review.EditConflict",
        "다른 요청이 먼저 이 리뷰를 수정했습니다. 리뷰를 다시 불러온 뒤 수정해 주세요.",
        ErrorType.Conflict);

    // RV-02 4b·RV-03 3b′. 403 — 리뷰는 공개 정보라 존재를 숨기지 않는다(ADR-009 결정 4).
    public static readonly Error NotReviewAuthor = new(
        "Review.NotAuthor",
        "본인이 작성한 리뷰만 수정하거나 삭제할 수 있습니다.",
        ErrorType.Forbidden);
}
