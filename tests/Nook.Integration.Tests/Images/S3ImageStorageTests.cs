using Amazon.S3;
using Nook.Infrastructure.Images;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Images;

// 운영 스토리지(S3)를 S3 호환 서버로 확인한다. LocalDiskImageStorageTests와 같은 계약 + S3에서만 생기는 경우(없는 키 → 404 매핑).
public sealed class S3ImageStorageTests(S3MockFixture s3Mock) : IClassFixture<S3MockFixture>, IDisposable
{
    // 테스트 목록
    //
    // - PutThenOpen_ReturnsSameBytesAndContentType     (리뷰·프로필 사진 prefix 둘 다)
    // - Open_MissingKey_ReturnsNull                     (SDK 예외 404 → null, ImagesController가 404로)
    // - Delete_RemovesObject_AndIsIdempotent
    // - Put_WithKeyNotOurShape_Throws_AndUploadsNothing (호출 경로 결함)
    // - Open_WithKeyNotOurShape_ReturnsNull_EvenIfObjectExists (불신 입력 — S3에 닿지 않는다)

    private static readonly byte[] Bytes = "RIFF\0\0\0\0WEBPdata"u8.ToArray();

    private readonly IAmazonS3 _s3 = s3Mock.CreateClient();
    private S3ImageStorage Storage => new(_s3, S3MockFixture.Bucket);

    public void Dispose() => _s3.Dispose();

    // 테스트끼리 같은 버킷을 쓴다 — 키마다 새 v4 hex를 넣어 서로 겹치지 않게 한다.
    private static string NewKey(string prefix = "reviews", string ext = "webp") =>
        $"{prefix}/2026/11/09/{Guid.NewGuid():N}.{ext}";

    [Theory]
    [InlineData("reviews", "webp", "image/webp")]
    [InlineData("avatars", "png", "image/png")]
    public async Task PutThenOpen_ReturnsSameBytesAndContentType(string prefix, string ext, string contentType)
    {
        var key = NewKey(prefix, ext);
        await Storage.PutAsync(key, new MemoryStream(Bytes), contentType, CancellationToken.None);

        var image = await Storage.OpenAsync(key, CancellationToken.None);

        Assert.NotNull(image);
        await using var content = image.Content;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy);
        Assert.Equal(Bytes, copy.ToArray());
        Assert.Equal(contentType, image.ContentType);
    }

    [Fact]
    public async Task Open_MissingKey_ReturnsNull()
    {
        Assert.Null(await Storage.OpenAsync(NewKey(), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_RemovesObject_AndIsIdempotent()
    {
        var key = NewKey();
        await Storage.PutAsync(key, new MemoryStream(Bytes), "image/webp", CancellationToken.None);

        await Storage.DeleteAsync(key, CancellationToken.None);
        await Storage.DeleteAsync(key, CancellationToken.None);

        Assert.Null(await Storage.OpenAsync(key, CancellationToken.None));
    }

    [Theory]
    [InlineData("../outside.webp")]
    [InlineData("reviews/2026/11/09/../../../../outside.webp")]
    public async Task Put_WithKeyNotOurShape_Throws_AndUploadsNothing(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Storage.PutAsync(key, new MemoryStream(Bytes), "image/webp", CancellationToken.None));

        var listed = await _s3.ListObjectsV2Async(new() { BucketName = S3MockFixture.Bucket, Prefix = "../" });
        Assert.Empty(listed.S3Objects ?? []);
    }

    // 객체가 실제로 있어도 null이어야 한다 — 없는 키는 검사가 없어도 404 → null이라 그것만으로는 검사가 일하는지 모른다.
    [Theory]
    [InlineData("reviews/2026/11/09/0123456789abcdef0123456789abcdef.exe")]
    [InlineData("private/2026/11/09/0123456789abcdef0123456789abcdef.webp")]
    public async Task Open_WithKeyNotOurShape_ReturnsNull_EvenIfObjectExists(string key)
    {
        await _s3.PutObjectAsync(new() { BucketName = S3MockFixture.Bucket, Key = key, InputStream = new MemoryStream(Bytes) });

        Assert.Null(await Storage.OpenAsync(key, CancellationToken.None));
    }
}
