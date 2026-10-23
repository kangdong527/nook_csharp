using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Nook.Api.Auth;
using Nook.Application.Reviews;
using Nook.SharedKernel;

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
        // IFormFile 스트림은 요청 본문을 버퍼링한 것이라 되감을 수 있다(ReviewImageFile 전제).
        var files = request.Images ?? [];
        var images = files.Select(f => new ReviewImageFile(f.OpenReadStream(), f.Length)).ToList();
        try
        {
            var command = new CreateReviewCommand(User.GetUserId(), request.PlaceId!.Value, request.Content, images);
            var result = await handler.HandleAsync(command, ct);

            if (!result.TryGetValue(out var review, out var error))
                return Problem(statusCode: StatusCodeOf(error), title: error.Code, detail: error.Description);

            return Created($"/reviews/{review.Id}", review);
        }
        finally
        {
            foreach (var image in images)
                await image.Content.DisposeAsync();
        }
    }

    private static int StatusCodeOf(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };
}

// multipart 폼. PlaceId는 Guid?+[Required] — 빠지면 Guid.Empty로 바인딩되어 조용히 통과하는 것을 400으로 막는다(PlaceRequest와 같은 이유).
public sealed class CreateReviewRequest
{
    [Required]
    public Guid? PlaceId { get; init; }

    public string? Content { get; init; }

    public List<IFormFile>? Images { get; init; }
}
