using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Api.Http;
using Nook.Application.Reviews;

namespace Nook.Api.Controllers;

// 리뷰 쓰기. 인가 표시 없음 → Fallback(로그인 필요, 1a).
[ApiController]
[Route("reviews")]
public sealed class ReviewsController : ControllerBase
{
    // RV-01. 201 + Location(ADR-009 결정 2). 조회 엔드포인트(GET /reviews/{id})는 아직 없다 — Location은 자원 주소의 약속.
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ReviewView>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromForm] CreateReviewRequest request,
        [FromServices] CreateReviewHandler handler,
        CancellationToken ct)
    {
        var images = OpenImages(request.Images);
        try
        {
            var command = new CreateReviewCommand(User.GetUserId(), request.PlaceId!.Value, request.Content, images);
            var result = await handler.HandleAsync(command, ct);

            if (!result.TryGetValue(out var review, out var error))
                return this.Problem(error);

            return Created($"/reviews/{review.Id}", review);
        }
        finally
        {
            await DisposeAsync(images);
        }
    }

    // RV-02. 본문·이미지를 통째로 보낸다(최종 상태) → PUT. 200 + 수정된 리뷰.
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ReviewView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Edit(
        Guid id,
        [FromForm] EditReviewRequest request,
        [FromServices] EditReviewHandler handler,
        CancellationToken ct)
    {
        var images = OpenImages(request.Images);
        try
        {
            var command = new EditReviewCommand(User.GetUserId(), id, request.Version!.Value, request.Content, request.KeepImageIds ?? [], images);
            var result = await handler.HandleAsync(command, ct);

            if (!result.TryGetValue(out var review, out var error))
                return this.Problem(error);

            return Ok(review);
        }
        finally
        {
            await DisposeAsync(images);
        }
    }

    // RV-03. 멱등 — 없는 리뷰·이미 지운 리뷰도 200 changed=false, 남의 리뷰만 403(ADR-009 결정 3·4).
    [HttpDelete("{id:guid}")]
    [ProducesResponseType<DeleteReviewView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteReviewHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new DeleteReviewCommand(User.GetUserId(), id), ct);

        if (!result.TryGetValue(out var view, out var error))
            return this.Problem(error);

        return Ok(view);
    }

    // IFormFile 스트림은 요청 본문을 버퍼링한 것이라 되감을 수 있다(ReviewImageFile 전제).
    private static List<ReviewImageFile> OpenImages(List<IFormFile>? files) =>
        [.. (files ?? []).Select(f => new ReviewImageFile(f.OpenReadStream(), f.Length))];

    private static async Task DisposeAsync(List<ReviewImageFile> images)
    {
        foreach (var image in images)
            await image.Content.DisposeAsync();
    }
}

// multipart 폼. PlaceId는 Guid?+[Required] — 빠지면 Guid.Empty로 바인딩되어 조용히 통과하는 것을 400으로 막는다(PlaceRequest와 같은 이유).
public sealed class CreateReviewRequest
{
    [Required]
    public Guid? PlaceId { get; init; }

    public string? Content { get; init; }

    public List<IFormFile>? Images { get; init; }
}

// RV-02 multipart 폼. KeepImageIds의 순서가 남길 이미지의 순서, Images는 그 뒤에 붙는다. 둘 다 비면 이미지 0장.
// Version은 필수 — 빠지면 0으로 바인딩되어 "버전 0을 본 화면"으로 통과하는 것을 400으로 막는다(PlaceId와 같은 이유).
public sealed class EditReviewRequest
{
    [Required]
    public int? Version { get; init; }

    public string? Content { get; init; }

    public List<Guid>? KeepImageIds { get; init; }

    public List<IFormFile>? Images { get; init; }
}
