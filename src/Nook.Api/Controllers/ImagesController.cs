using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nook.Application.Reviews;

namespace Nook.Api.Controllers;

// 리뷰 이미지 표시(FE-2 이미지 A안). 익명 허용 — <img>는 Bearer 헤더를 보낼 수 없고(BFF 결정),
// 키에 128비트 랜덤(v4 32자 hex)이 들어 있어 키를 아는 사람만 본다(S3 공개 객체 URL과 같은 모델).
// 키는 불신 입력: 우리가 만드는 모양(ReviewImageKeys)과 다르면 스토리지에 넘기지 않고 404.
[ApiController]
[Route("images")]
[AllowAnonymous]
public sealed class ImagesController(IReviewImageStorage storage) : ControllerBase
{
    [HttpGet("{**key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        if (!ReviewImageKeys.IsValid(key) || await storage.OpenAsync(key, ct) is not { } image)
            return NotFound();

        // 키는 한 번 정해지면 내용이 바뀌지 않는다(교체 = 새 키, D2-4) → 오래 캐시해도 된다.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Content, image.ContentType);
    }
}
