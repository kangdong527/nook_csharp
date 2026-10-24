using Nook.SharedKernel;

namespace Nook.Application.Bookmarks;

public static class BookmarkErrors
{
    // BM-01·02 2b. 장소 존재는 다른 애그리거트를 봐야 알 수 있어 Domain이 아니라 Application의 실패다.
    public static readonly Error PlaceNotFound = new(
        "Bookmark.PlaceNotFound",
        "북마크할 장소를 찾을 수 없습니다.",
        ErrorType.NotFound);
}
