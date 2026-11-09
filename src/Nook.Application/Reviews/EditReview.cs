using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nook.Application.Images;
using Nook.Domain.Reviews;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Reviews;

// RV-02 리뷰를 수정한다. UserId는 토큰의 sub, ReviewId는 경로, 나머지는 multipart 폼 값.
// 이미지 최종 순서 = KeepImageIds 순서 → 그 뒤에 Images 순서(usecases.md RV-02 1단계).
// Version: 수정 화면이 받아 둔 리뷰 버전. 화면을 연 사이 다른 수정이 커밋됐으면 6c(incident 002 미결 ②).
public sealed record EditReviewCommand(
    Guid UserId,
    Guid ReviewId,
    int Version,
    string? Content,
    IReadOnlyList<Guid> KeepImageIds,
    IReadOnlyList<ReviewImageFile> Images);

// 트랜잭션 경계: 업로드는 밖(앞), 리뷰 행 + 이미지 행 변경은 TrySaveAsync의 SaveChanges 한 번,
// 빠진 이미지의 스토리지 삭제는 밖(뒤, 커밋 후) — ADR-009 결정 1, CLAUDE.md.
// 순서: DB 없는 검사 → 조회·권한 → Edit(메모리만 변경) → 업로드 → 저장 → 삭제. 거절은 모두 업로드 전에 끝난다.
public sealed class EditReviewHandler(
    IReviewRepository reviews,
    IImageStorage storage,
    IReviewImageUrls urls,
    TimeProvider time,
    ILogger<EditReviewHandler> logger)
{
    public async Task<Result<ReviewView>> HandleAsync(EditReviewCommand command, CancellationToken ct)
    {
        // 2단계(2a)·이미지 형식·용량. 경로·폼의 Guid.Empty는 From이 예외 → 500(SaveBookmark와 같은 사용자 결정).
        if (!ReviewContent.Create(command.Content).TryGetValue(out var content, out var contentError))
            return contentError;

        if (!(await ReviewImageUploads.ValidateAsync(command.Images, ct)).TryGetValue(out var formats, out var imageError))
            return imageError;

        var reviewId = ReviewId.From(command.ReviewId);
        var keepImageIds = command.KeepImageIds.Select(ReviewImageId.From).ToList();

        // 4단계(4a·4b).
        var review = await reviews.GetAsync(reviewId, ct);
        if (review is null)
            return ReviewWriteErrors.ReviewNotFound;

        if (review.UserId != UserId.From(command.UserId))
            return ReviewWriteErrors.NotReviewAuthor;

        // 6c 사전 확인: 화면을 연 사이 다른 수정이 커밋됐다. 업로드 전에 거절해 고아를 만들지 않는다.
        // 통과하면 읽은 Version = 화면의 Version이므로, 이 확인과 저장 사이의 경합은 TrySaveAsync의 version WHERE가 최종 판정한다.
        // 권한(4b) 뒤에 둔다 — 남의 리뷰에 409를 주면 "그사이 수정됐다"는 사실이 새어 나간다.
        if (command.Version != review.Version)
            return ReviewWriteErrors.EditConflict;

        // 3c·4c는 Edit가 판단한다. 키를 미리 만들어 업로드 전에 Edit를 부르므로, 거절돼도 올라간 이미지가 없다.
        // Edit는 추적 중인 애그리거트를 메모리에서만 바꾼다 — 업로드가 실패하면(5c, 예외) SaveChanges 없이 요청 범위와 함께 버려진다.
        var newKeys = ReviewImageUploads.NewKeys(formats, time.GetUtcNow());
        if (!review.Edit(content, keepImageIds, newKeys).TryGetValue(out var removedKeys, out var editError))
            return editError;

        // 5단계.
        await ReviewImageUploads.UploadAsync(storage, command.Images, formats, newKeys, ct);

        // 6단계.
        var outcome = await reviews.TrySaveAsync(review, ct);
        switch (outcome)
        {
            case SaveReviewOutcome.Saved:
                await ReviewImageCleanup.DeleteAfterCommitAsync(storage, removedKeys, logger);
                // 7단계. 리뷰가 있으니 R-STAR는 fill(RV-01 7단계와 같은 이유).
                return ReviewView.From(review, urls, StarRule.Evaluate(isBookmarked: false, hasReview: true));

            // 6b. 지워진 리뷰라 removedKeys는 RV-03이 이미 맡았고, 방금 올린 newKeys는 고아 — 배치가 정리한다(ADR-009).
            case SaveReviewOutcome.ReviewGone:
                return ReviewWriteErrors.ReviewNotFound;

            // 6c. 저장이 롤백돼 removedKeys는 아직 쓰이는 중일 수 있다 — 지우지 않는다. newKeys는 고아(배치).
            case SaveReviewOutcome.Conflict:
                return ReviewWriteErrors.EditConflict;

            default:
                throw new UnreachableException($"처리하지 않은 {nameof(SaveReviewOutcome)}: {outcome}");
        }
    }
}
