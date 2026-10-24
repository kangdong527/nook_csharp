using Nook.Domain.Reviews;
using Nook.Domain.Stars;

namespace Nook.Application.Reviews;

// RV-01·RV-02 응답(ADR-009 결정 2). Star는 BookmarkView와 같은 표기(enum 이름 소문자).
// Images: sortOrder 순. Id는 RV-02의 KeepImageIds로 돌려받는 식별자, Key는 표시용 — 키 → URL 변환은 한 곳에서(erd.md §3),
// 실제 스토리지가 붙는 DEP 주간에 URL로 바꾼다. 식별자를 Key로 두지 않는 이유가 이것(ADR-009 결정 2 개정).
public sealed record ReviewView(
    Guid Id,
    Guid PlaceId,
    string Content,
    IReadOnlyList<ReviewImageView> Images,
    DateTimeOffset CreatedAt,
    string Star)
{
    internal static ReviewView From(Review review, StarState star) =>
        new(
            review.Id.Value,
            review.PlaceId.Value,
            review.Content.Value,
            [.. review.Images.Select(i => new ReviewImageView(i.Id.Value, i.StorageKey))],
            review.CreatedAt,
            star.ToString().ToLowerInvariant());
}

public sealed record ReviewImageView(Guid Id, string Key);
