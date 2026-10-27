using System.Net;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// GET /images/{key}(FE-2 이미지 A안). 익명 허용이므로 "키 모양이 아니면 스토리지에 닿지 않는다"가 핵심이다.
public sealed class ImagesApiTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // - Get_ExistingKey_AnonymousReturnsBytesTypeAndLongCache
    // - Get_ValidShapeButMissing_Returns404
    // - Get_KeyNotOurShape_Returns404WithoutTouchingStorage   (경로 조작·대문자 hex·다른 확장자·다른 prefix)

    private const string Key = "reviews/2026/10/27/0123456789abcdef0123456789abcdef.png";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private readonly NookApiFactory _factory;
    private readonly HttpClient _anonymous;

    public ImagesApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _anonymous = _factory.CreateClient();   // 토큰 없음 — <img> 요청과 같다
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Get_ExistingKey_AnonymousReturnsBytesTypeAndLongCache()
    {
        await _factory.ImageStorage.PutAsync(Key, new MemoryStream(Png), "image/png", CancellationToken.None);

        var response = await _anonymous.GetAsync($"/images/{Key}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Get_ValidShapeButMissing_Returns404()
    {
        var response = await _anonymous.GetAsync($"/images/{Key}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    // 앞 두 줄(..)은 HttpClient·라우팅의 URL 정규화가 먼저 막는다 — 키 모양 검사를 지우는 변이에도 404였다(FE-2 확인).
    // 모양 검사를 지키는 것은 아래 세 줄이다(변이 시 빨간불).
    [InlineData("reviews/2026/10/27/../../../appsettings.json")]
    [InlineData("reviews/2026/10/27/%2e%2e%2f%2e%2e%2fsecret.png")]
    [InlineData("reviews/2026/10/27/0123456789ABCDEF0123456789ABCDEF.png")]   // 대문자 hex — 우리가 만들지 않는 모양
    [InlineData("reviews/2026/10/27/0123456789abcdef0123456789abcdef.gif")]
    [InlineData("photos/2026/10/27/0123456789abcdef0123456789abcdef.png")]    // 우리가 쓰지 않는 prefix(reviews·avatars만)
    public async Task Get_KeyNotOurShape_Returns404WithoutTouchingStorage(string key)
    {
        // 같은 이름의 객체가 메모리에 있어도 모양 검사에서 먼저 막혀야 한다.
        await _factory.ImageStorage.PutAsync(Uri.UnescapeDataString(key), new MemoryStream(Png), "image/png", CancellationToken.None);

        var response = await _anonymous.GetAsync($"/images/{key}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
