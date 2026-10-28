using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Api.Http;
using Nook.Application.Bookmarks;
using Nook.Application.Groups;
using Nook.Application.Reviews;
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

    // PF-01 닉네임. 400 형식 · 409 중복(User.NicknameTaken) · 401 회원 없음. 사진은 PUT/DELETE /me/avatar(따로 — 요청 형식이 다르다).
    [HttpPatch]
    [ProducesResponseType<MeView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(UpdateMeRequest request, [FromServices] ChangeNicknameHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ChangeNicknameCommand(User.GetUserId(), request.Nickname), ct);

        return result.TryGetValue(out var me, out var error) ? Ok(me) : this.Problem(error);
    }

    // PF-01 프로필 사진 교체. multipart 필드 Image 한 장. 400 크기·형식 · 401 회원 없음. 200 + 바뀐 MeView(avatarUrl).
    [HttpPut("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<MeView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeAvatar(
        [FromForm] ChangeAvatarRequest request,
        [FromServices] ChangeAvatarHandler handler,
        CancellationToken ct)
    {
        await using var content = request.Image!.OpenReadStream();
        var result = await handler.HandleAsync(
            new ChangeAvatarCommand(User.GetUserId(), new ReviewImageFile(content, request.Image.Length)), ct);

        return result.TryGetValue(out var me, out var error) ? Ok(me) : this.Problem(error);
    }

    // PF-01 프로필 사진 삭제 → 기본 이미지. 멱등(없어도 200).
    [HttpDelete("avatar")]
    [ProducesResponseType<MeView>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveAvatar([FromServices] RemoveAvatarHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(User.GetUserId(), ct);

        return result.TryGetValue(out var me, out var error) ? Ok(me) : this.Problem(error);
    }

    // PF-02 회원 탈퇴. 204. 이미 탈퇴했으면 401(AU-04와 같다). 리뷰·좋아요는 남는다(PM). 쿠키 삭제는 BFF(/api/auth/withdraw)가 한다.
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Withdraw([FromServices] WithdrawHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(User.GetUserId(), ct);

        return result.IsFailure(out var error) ? this.Problem(error) : NoContent();
    }

    // PF-04. 첫 요청은 cursor 없이, 다음부터는 직전 응답의 nextCursor 그대로. 기본 5개(유스케이스 "5개씩 더 보기").
    [HttpGet("reviews")]
    [ProducesResponseType<MyReviewPageView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reviews(
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        [FromServices] GetMyReviewsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetMyReviewsQuery(User.GetUserId(), cursor, limit), ct);

        return result.TryGetValue(out var page, out var error) ? Ok(page) : this.Problem(error);
    }

    // GR-06 내 그룹 목록(합류 순, 최대 3개). 없으면 빈 배열.
    [HttpGet("groups")]
    [ProducesResponseType<IReadOnlyList<MyGroupView>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Groups([FromServices] GetMyGroupsHandler handler, CancellationToken ct) =>
        Ok(await handler.HandleAsync(new GetMyGroupsQuery(User.GetUserId()), ct));

    // BM-03. 없으면 빈 배열(200).
    [HttpGet("places")]
    [ProducesResponseType<IReadOnlyList<MyPlaceView>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Places([FromServices] GetMyPlacesHandler handler, CancellationToken ct) =>
        Ok(await handler.HandleAsync(new GetMyPlacesQuery(User.GetUserId()), ct));
}

public sealed record UpdateMeRequest(string? Nickname);

public sealed class ChangeAvatarRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public IFormFile? Image { get; init; }
}
