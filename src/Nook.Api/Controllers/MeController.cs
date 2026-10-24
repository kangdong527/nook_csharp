using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Application.Bookmarks;

namespace Nook.Api.Controllers;

// 로그인한 회원 자신의 자원. 경로에 회원 ID를 받지 않는다 — 토큰의 sub가 곧 "나"라 남의 것을 요청할 길이 없다.
// 인가 표시 없음 → Fallback(로그인 필요).
[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    // BM-03. 없으면 빈 배열(200).
    [HttpGet("places")]
    [ProducesResponseType<IReadOnlyList<MyPlaceView>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Places([FromServices] GetMyPlacesHandler handler, CancellationToken ct) =>
        Ok(await handler.HandleAsync(new GetMyPlacesQuery(User.GetUserId()), ct));
}
