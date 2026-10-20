using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nook.Application.Auth;

namespace Nook.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    // AU-01·AU-02 흐름 1. 가입 여부·속도 제한과 무관하게 202(D5 결정).
    // 형식 오류만 400 — DB를 보지 않고 판단하므로 가입 여부가 드러나지 않는다(D5-2).
    [AllowAnonymous]
    [HttpPost("magic-link")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestMagicLink(
        MagicLinkRequest request,
        [FromServices] RequestMagicLinkHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new RequestMagicLinkCommand(request.Email), ct);

        if (result.IsFailure(out var error))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Code, detail: error.Description);

        return Accepted();
    }
}

public sealed record MagicLinkRequest(string? Email);
