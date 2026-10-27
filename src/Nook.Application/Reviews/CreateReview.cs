using Nook.Application.Places;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;
using System.Diagnostics;

namespace Nook.Application.Reviews;

// RV-01 리뷰를 작성한다. UserId는 토큰의 sub, 나머지는 multipart 폼 값.
public sealed record CreateReviewCommand(
    Guid UserId,
    Guid PlaceId,
    string? Content,
    IReadOnlyList<ReviewImageFile> Images);

// 트랜잭션 경계: 업로드(외부 I/O)는 밖, 리뷰+이미지 행은 TryAddAsync의 SaveChanges 한 번(ADR-009, CLAUDE.md).
// 순서: DB 없는 검사 → DB 조회 → 업로드 → 저장. 업로드 전에 거절할수록 고아 이미지가 줄어든다.
public sealed class CreateReviewHandler(
    IPlaceRepository places,
    IReviewRepository reviews,
    IReviewImageStorage storage,
    IReviewImageUrls urls,
    TimeProvider time)
{
    public async Task<Result<ReviewView>> HandleAsync(CreateReviewCommand command, CancellationToken ct)
    {
        // 2단계(2a·2b·2c). 이미지는 전부 검증한 뒤에만 올린다 — 3번째가 틀렸을 때 1·2번째가 올라가 있으면 안 된다.
        if (!ReviewContent.Create(command.Content).TryGetValue(out var content, out var contentError))
            return contentError;

        if (command.Images.Count > Review.MaxImages)
            return ReviewErrors.TooManyImages;

        if (!(await ReviewImageUploads.ValidateAsync(command.Images, ct)).TryGetValue(out var formats, out var imageError))
            return imageError;

        // 3단계(3a).
        var placeId = PlaceId.From(command.PlaceId);
        if (!await places.ExistsAsync(placeId, ct))
            return ReviewWriteErrors.PlaceNotFound;

        // "지금"은 여기서 한 번만 정한다(ADR-007). 사전 확인 날짜·이미지 키·Review.CreatedAt이 모두 이 값을 쓴다.
        // 날짜는 Review 생성자(ReviewDate)와 같은 식으로 계산해야 사전 확인과 UNIQUE가 같은 날을 본다. GetLocalNow는 서버 시간대를 탄다.
        var now = time.GetUtcNow();
        var reviewDate = DateOnly.FromDateTime(now.UtcDateTime);

        // 4단계(4a). 업로드 전에 걸러 고아 이미지를 줄이는 용도 — 최종 판정은 6단계의 UNIQUE.
        var userId = UserId.From(command.UserId);
        if (await reviews.ExistsOnDateAsync(userId, placeId, reviewDate, ct))
            return ReviewWriteErrors.AlreadyReviewedToday;

        // 5단계. 업로드 실패(5a)는 잡지 않는다 — 예외 → 500. 먼저 올라간 이미지는 배치가 정리한다(ADR-009).
        var storageKeys = ReviewImageUploads.NewKeys(formats, now);
        await ReviewImageUploads.UploadAsync(storage, command.Images, formats, storageKeys, ct);

        // 6단계. 입력은 2단계에서 모두 검증했다. 여기서 실패하면 Review와 핸들러의 규칙이 어긋난 버그다.
        if (!Review.Create(userId, placeId, content, storageKeys, now).TryGetValue(out var review, out var reviewError))
            throw new UnreachableException($"검증을 통과한 입력으로 Review.Create가 실패했다: {reviewError.Code}");

        var outcome = await reviews.TryAddAsync(review, ct);

        return outcome switch
        {
            // 7단계. 방금 쓴 리뷰가 있으니 R-STAR는 북마크와 무관하게 fill — 북마크를 조회하지 않는다.
            AddReviewOutcome.Added => ReviewView.From(review, urls, StarRule.Evaluate(isBookmarked: false, hasReview: true)),
            // 6a. 진 쪽 이미지는 고아로 남고 배치가 정리한다(ADR-009).
            AddReviewOutcome.AlreadyReviewedToday => ReviewWriteErrors.AlreadyReviewedToday,
            _ => throw new UnreachableException($"처리하지 않은 {nameof(AddReviewOutcome)}: {outcome}"),
        };
    }
}
