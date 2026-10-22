using Nook.Domain.Stars;

namespace Nook.Application.Bookmarks;

// BM-01·02 응답. Changed=false는 실패가 아니라 "이미 그 상태"(2a, 사용자 결정: 멱등 성공).
// Star는 문자열: Api는 Domain(StarState)을 볼 수 없다(ADR-001 결정 3). 값은 enum 이름의 소문자 — R-STAR 표기와 같다.
// 확장될 수 있는 값이다 — 클라이언트는 모르는 값을 empty로 다룬다(D5-5 계약).
public sealed record BookmarkView(bool Bookmarked, bool Changed, string Star)
{
    internal static BookmarkView From(bool bookmarked, bool changed, StarState star) =>
        new(bookmarked, changed, star.ToString().ToLowerInvariant());
}
