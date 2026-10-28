using Nook.Domain.Reviews;
using Nook.Domain.Stars;

namespace Nook.Application.Reviews;

// RV-01·RV-02 응답(ADR-009 결정 2). Star는 BookmarkView와 같은 표기(enum 이름 소문자).
// Images: sortOrder 순. Id는 RV-02의 KeepImageIds로 돌려받는 식별자, Key는 표시용 — 키 → URL 변환은 한 곳에서(erd.md §3),
// 실제 스토리지가 붙는 DEP 주간에 URL로 바꾼다. 식별자를 Key로 두지 않는 이유가 이것(ADR-009 결정 2 개정).
// Version: 불투명 값 — 클라이언트는 해석하지 않고 RV-02 요청에 그대로 돌려보낸다(incident 002 미결 ②).
public sealed record ReviewView(
    Guid Id,
    Guid PlaceId,
    string Content,
    IReadOnlyList<ReviewImageView> Images,
    DateTimeOffset CreatedAt,
    int Version,
    string Star)
{
    internal static ReviewView From(Review review, IReviewImageUrls urls, StarState star) =>
        new(
            review.Id.Value,
            review.PlaceId.Value,
            review.Content.Value,
            [.. review.Images.Select(i => ReviewImageView.From(i.Id.Value, i.StorageKey, urls))],
            review.CreatedAt,
            review.Version,
            star.ToString().ToLowerInvariant());
}

// Url: 화면이 그대로 쓰는 주소(IReviewImageUrls가 만든다, FE-2). Key는 RV-02 이전 클라이언트 호환·디버깅용으로 남긴다.
public sealed record ReviewImageView(Guid Id, string Key, string Url)
{
    internal static ReviewImageView From(Guid id, string key, IReviewImageUrls urls) => new(id, key, urls.For(key));
}
