using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Api.Http;
using Nook.Application.Bookmarks;
using Nook.Application.Users;

namespace Nook.Api.Controllers;

// 로그인한 회원 자신의 자원. 경로에 회원 ID를 받지 않는다 — 토큰의 sub가 곧 "나"라 남의 것을 요청할 길이 없다.
// 인가 표시 없음 → Fallback(로그인 필요).
[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    // AU-04. 회원 행이 없으면 401(Auth.UnknownUser) — 미들웨어 401과 title로 구분된다.
    [HttpGet]
    [ProducesResponseType<MeView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get([FromServices] GetMeHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetMeQuery(User.GetUserId()), ct);

        return result.TryGetValue(out var me, out var error)
            ? Ok(me)
            : this.Problem(error);
    }

    // BM-03. 없으면 빈 배열(200).
    [HttpGet("places")]
    [ProducesResponseType<IReadOnlyList<MyPlaceView>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Places([FromServices] GetMyPlacesHandler handler, CancellationToken ct) =>
        Ok(await handler.HandleAsync(new GetMyPlacesQuery(User.GetUserId()), ct));
}
