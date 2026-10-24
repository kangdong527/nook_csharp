using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.SharedKernel;

namespace Nook.Api.Controllers;

// 인가 표시 없음 → Fallback 정책(로그인 필요, PL-01 1b).
[ApiController]
[Route("places")]
public sealed class PlacesController : ControllerBase
{
    // PL-02 장소 상세 + 내 별.
    [HttpGet("{placeId:guid}")]
    [ProducesResponseType<PlaceDetailView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid placeId, [FromServices] GetPlaceHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetPlaceQuery(User.GetUserId(), placeId), ct);

        return result.TryGetValue(out var view, out var error)
            ? Ok(view)
            : Problem(statusCode: StatusCodes.Status404NotFound, title: error.Code, detail: error.Description);
    }

    // PL-02 리뷰 목록(무한 스크롤). 첫 요청은 cursor 없이, 다음부터는 직전 응답의 nextCursor를 그대로.
    [HttpGet("{placeId:guid}/reviews")]
    [ProducesResponseType<ReviewPageView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reviews(
        Guid placeId,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        [FromServices] GetPlaceReviewsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetPlaceReviewsQuery(User.GetUserId(), placeId, cursor, limit), ct);

        if (result.TryGetValue(out var page, out var error))
            return Ok(page);

        var status = error.Type == ErrorType.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        return Problem(statusCode: status, title: error.Code, detail: error.Description);
    }

    // PL-01. 새로 만들었든 기존 것이든 같은 응답(200) — 호출자는 "확정된 장소"만 필요하고, 몇 번 불러도 결과가 같다.
    [HttpPost]
    [ProducesResponseType<PlaceView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPlace(
        ConfirmPlaceRequest request,
        [FromServices] ConfirmPlaceHandler handler,
        CancellationToken ct)
    {
        // [Required] 덕분에 여기서 좌표는 null이 아니다([ApiController]가 먼저 400).
        var command = new ConfirmPlaceCommand(
            request.KakaoPlaceId, request.Name, request.Address, request.Latitude!.Value, request.Longitude!.Value);

        var result = await handler.HandleAsync(command, ct);

        if (!result.TryGetValue(out var place, out var error))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Code, detail: error.Description);

        return Ok(place);
    }
}

// 좌표가 double이면 JSON에서 빠졌을 때 0으로 바인딩된다 — (0, 0)은 유효한 좌표라 조용히 통과한다.
// double? + [Required]로 "빠짐"을 400으로 만든다(PL-01 1a).
// 대상 지정 없이 매개변수에 붙인다 — MVC는 위치 record의 검증 특성을 생성자 매개변수에서 읽고, [property:]로 붙이면 시작 시 예외.
public sealed record ConfirmPlaceRequest(
    string? KakaoPlaceId,
    string? Name,
    string? Address,
    [Required] double? Latitude,
    [Required] double? Longitude);
