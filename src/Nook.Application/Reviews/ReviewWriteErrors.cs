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
}
