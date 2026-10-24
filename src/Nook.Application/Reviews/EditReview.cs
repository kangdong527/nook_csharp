using Microsoft.Extensions.Logging;
using Nook.Domain.Reviews;
using Nook.Domain.Stars;
using Nook.Domain.Users;
using Nook.SharedKernel;
using System.Diagnostics;

namespace Nook.Application.Reviews;

// RV-02 리뷰를 수정한다. UserId는 토큰의 sub, ReviewId는 경로, 나머지는 multipart 폼 값.
// 이미지 최종 순서 = KeepImageIds 순서 → 그 뒤에 Images 순서(usecases.md RV-02 1단계).
public sealed record EditReviewCommand(
    Guid UserId,
    Guid ReviewId,
    string? Content,
    IReadOnlyList<Guid> KeepImageIds,
    IReadOnlyList<ReviewImageFile> Images);

// 트랜잭션 경계: 업로드는 밖(앞), 리뷰 행 + 이미지 행 변경은 TrySaveAsync의 SaveChanges 한 번,
// 빠진 이미지의 스토리지 삭제는 밖(뒤, 커밋 후) — ADR-009 결정 1, CLAUDE.md.
// 순서: DB 없는 검사 → 조회·권한 → Edit(메모리만 변경) → 업로드 → 저장 → 삭제. 거절은 모두 업로드 전에 끝난다.
public sealed class EditReviewHandler(
    IReviewRepository reviews,
    IReviewImageStorage storage,
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
                await DeleteRemovedAsync(removedKeys);
                // 7단계. 리뷰가 있으니 R-STAR는 fill(RV-01 7단계와 같은 이유).
                return ReviewView.From(review, StarRule.Evaluate(isBookmarked: false, hasReview: true));

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

    // 커밋 후 정리. 수정은 이미 성공했으므로 실패해도 응답을 바꾸지 않는다 — 남은 객체는 배치가 정리(ADR-009).
    // 요청 ct를 넘기지 않는다: 커밋 뒤 클라이언트가 끊겨도 취소 예외가 성공한 수정을 500으로 바꾸면 안 된다.
    private async Task DeleteRemovedAsync(IReadOnlyList<string> keys)
    {
        foreach (var key in keys)
        {
            try
            {
                await storage.DeleteAsync(key, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "리뷰 이미지 삭제 실패, 배치가 정리한다: {StorageKey}", key);
            }
        }
    }
}
