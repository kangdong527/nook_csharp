using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Api.Http;
using Nook.Application.Bookmarks;

namespace Nook.Api.Controllers;

// BM-01·02. 토글 대신 PUT/DELETE(사용자 결정): 같은 요청을 두 번 보내도 결과가 같다(멱등) — 따닥 클릭(1b)이 상태를 뒤집지 않는다.
// "이미 그 상태"도 200 + changed=false(사용자 결정 (a)). 인가 표시 없음 → Fallback(로그인 필요, 1a).
[ApiController]
[Route("places/{placeId:guid}/bookmark")]
public sealed class BookmarksController : ControllerBase
{
    [HttpPut]
    [ProducesResponseType<BookmarkView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(Guid placeId, [FromServices] SaveBookmarkHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new SaveBookmarkCommand(User.GetUserId(), placeId), ct);

        return result.TryGetValue(out var view, out var error)
            ? Ok(view)
            : this.Problem(error);
    }

    [HttpDelete]
    [ProducesResponseType<BookmarkView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid placeId, [FromServices] RemoveBookmarkHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RemoveBookmarkCommand(User.GetUserId(), placeId), ct);

        return result.TryGetValue(out var view, out var error)
            ? Ok(view)
            : this.Problem(error);
    }
}
