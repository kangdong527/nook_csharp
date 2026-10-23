using Nook.Domain.Reviews;
using Nook.Domain.Stars;

namespace Nook.Application.Reviews;

// RV-01 응답(ADR-009 결정 2). Star는 BookmarkView와 같은 표기(enum 이름 소문자).
// ImageKeys: 키 → URL 변환은 한 곳에서(erd.md §3) — 실제 스토리지가 붙는 DEP 주간에 URL로 바꾼다.
public sealed record ReviewView(
    Guid Id,
    Guid PlaceId,
    string Content,
    IReadOnlyList<string> ImageKeys,
    DateTimeOffset CreatedAt,
    string Star)
{
    internal static ReviewView From(Review review, StarState star) =>
        new(
            review.Id.Value,
            review.PlaceId.Value,
            review.Content.Value,
            [.. review.Images.Select(i => i.StorageKey)],
            review.CreatedAt,
            star.ToString().ToLowerInvariant());
}
