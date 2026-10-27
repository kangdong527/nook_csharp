using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Api.Http;
using Nook.Application.Likes;

namespace Nook.Api.Controllers;

// LK-01. 원본은 토글(toggleLike)이지만 BM과 같이 PUT/DELETE — 따닥 클릭이 상태를 뒤집지 않는다(멱등).
// 인가 표시 없음 → Fallback(로그인 필요).
[ApiController]
[Route("reviews/{reviewId:guid}/like")]
public sealed class LikesController : ControllerBase
{
    [HttpPut]
    [ProducesResponseType<LikeView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid reviewId, [FromServices] AddLikeHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new AddLikeCommand(User.GetUserId(), reviewId), ct);

        return result.TryGetValue(out var view, out var error)
            ? Ok(view)
            : this.Problem(error);
    }

    [HttpDelete]
    [ProducesResponseType<LikeView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid reviewId, [FromServices] RemoveLikeHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RemoveLikeCommand(User.GetUserId(), reviewId), ct);

        return result.TryGetValue(out var view, out var error)
            ? Ok(view)
            : this.Problem(error);
    }
}
