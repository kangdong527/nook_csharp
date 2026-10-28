using Nook.Application.Reviews;

namespace Nook.Infrastructure.Reviews;

// 기준 주소 + 키. 개발: "/api/nook/images/"(BFF 중계 — 브라우저는 C# API를 직접 부르지 않는다), DEP: S3·CDN 주소.
public sealed class PrefixReviewImageUrls(string baseUrl) : IReviewImageUrls
{
    private readonly string _base = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    public string For(string storageKey) => _base + storageKey;
}
