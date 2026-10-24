using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Application.Auth;
using Nook.SharedKernel;

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

    // AU-02 흐름 3. GET이 아니라 POST인 이유: 메일 보안 스캐너가 링크를 미리 열어도 소모되지 않게(erd.md 흐름 2).
    // 닉네임 필요는 오류가 아니라 가입의 다음 단계라 200 + needsNickname(D5-3 사용자 결정).
    [AllowAnonymous]
    [HttpPost("magic-link/consume")]
    [ProducesResponseType<ConsumeMagicLinkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConsumeMagicLink(
        ConsumeMagicLinkRequest request,
        [FromServices] ConsumeMagicLinkHandler handler,
        [FromServices] AccessTokenIssuer issuer,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ConsumeMagicLinkCommand(request.Token, request.Nickname), ct);

        if (!result.TryGetValue(out var outcome, out var error))
            return Problem(statusCode: ToStatusCode(error.Type), title: error.Code, detail: error.Description);

        // 토큰 발급은 커밋 뒤, AccessTokenIssuer 한 곳에서(D5 결정)
        return outcome.SignedInUserId is { } userId
            ? Ok(new ConsumeMagicLinkResponse(issuer.Issue(userId), NeedsNickname: false))
            : Ok(new ConsumeMagicLinkResponse(AccessToken: null, NeedsNickname: true));
    }

    // ErrorType → 상태 코드(D2-1: Domain은 HTTP를 모르고 Api가 번역한다). 컨트롤러가 늘면 공통 위치로 옮긴다.
    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}

public sealed record MagicLinkRequest(string? Email);

public sealed record ConsumeMagicLinkRequest(string? Token, string? Nickname);

public sealed record ConsumeMagicLinkResponse(string? AccessToken, bool NeedsNickname);
