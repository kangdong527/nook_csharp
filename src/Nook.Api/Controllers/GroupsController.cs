using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Application.Groups;
using Nook.SharedKernel;

namespace Nook.Api.Controllers;

// 그룹 쓰기. 인가 표시 없음 → Fallback(로그인 필요, 1a).
[ApiController]
[Route("groups")]
public sealed class GroupsController : ControllerBase
{
    // GR-02. 201 + Location(RV-01과 같은 형식). 조회 엔드포인트(GET /groups/{id})는 아직 없다 — Location은 자원 주소의 약속.
    [HttpPost]
    [ProducesResponseType<GroupView>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateGroupRequest request, [FromServices] CreateGroupHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CreateGroupCommand(User.GetUserId(), request.Name), ct);

        return result.TryGetValue(out var view, out var error)
            ? Created($"/groups/{view.Id}", view)
            : ProblemOf(error);
    }

    // GR-01. 클라이언트는 그룹 ID를 모르고 코드만 안다 → /groups/{id}/membership 모양을 쓸 수 없다.
    // 코드는 본문으로 — 경로에 두면 접근·프록시 로그에 그룹 출입 열쇠가 남는다(D6-2 사용자 결정).
    [HttpPost("join")]
    [ProducesResponseType<GroupView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(
        JoinGroupRequest request, [FromServices] JoinGroupHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new JoinGroupCommand(User.GetUserId(), request.InviteCode), ct);

        return result.TryGetValue(out var view, out var error) ? Ok(view) : ProblemOf(error);
    }

    // GR-04. 멱등 — 멤버가 아니거나 그룹이 없어도 204(R-GR-09, D6-1 사용자 결정). 좋아요·북마크의 DELETE와 같은 모양.
    [HttpDelete("{groupId:guid}/membership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Leave(Guid groupId, [FromServices] LeaveGroupHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new LeaveGroupCommand(User.GetUserId(), groupId), ct);

        return result.IsFailure(out var error) ? ProblemOf(error) : NoContent();
    }

    private ObjectResult ProblemOf(Error error) =>
        Problem(statusCode: StatusCodeOf(error), title: error.Code, detail: error.Description);

    private static int StatusCodeOf(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };
}

// 값 검증(빈 값·길이·형식)은 도메인 값 객체가 한다 — null도 그쪽에서 400(Validation)으로 거절된다.
public sealed class CreateGroupRequest
{
    public string? Name { get; init; }
}

public sealed class JoinGroupRequest
{
    public string? InviteCode { get; init; }
}
