using Nook.Infrastructure.Images;

namespace Nook.Integration.Tests.Images;

// 개발용 로컬 디스크 스토리지(FE-2). 실제 파일 시스템을 쓰므로 통합 테스트 쪽에 둔다(DB는 쓰지 않는다).
public sealed class LocalDiskImageStorageTests : IDisposable
{
    // 테스트 목록
    //
    // - PutThenOpen_ReturnsSameBytesAndContentType
    // - Delete_RemovesFile_AndIsIdempotent
    // - Put_WithKeyNotOurShape_Throws                 (호출 경로 결함 — 우리가 만든 키만 들어온다)
    // - Open_WithKeyNotOurShape_ReturnsNull           (불신 입력 — 디스크에 닿지 않는다)

    private const string Key = "reviews/2026/10/27/0123456789abcdef0123456789abcdef.webp";
    private static readonly byte[] Bytes = "RIFF\0\0\0\0WEBPdata"u8.ToArray();

    private readonly string _root = Path.Combine(Path.GetTempPath(), "nook-images-" + Guid.NewGuid().ToString("N"));
    private readonly LocalDiskImageStorage _storage;

    public LocalDiskImageStorageTests() => _storage = new LocalDiskImageStorage(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task PutThenOpen_ReturnsSameBytesAndContentType()
    {
        await _storage.PutAsync(Key, new MemoryStream(Bytes), "image/webp", CancellationToken.None);

        var image = await _storage.OpenAsync(Key, CancellationToken.None);

        Assert.NotNull(image);
        await using var content = image.Content;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy);
        Assert.Equal(Bytes, copy.ToArray());   // 배열은 튜플 안에 넣으면 참조 비교가 된다 — 따로 단언
        Assert.Equal("image/webp", image.ContentType);
        Assert.False(File.Exists(Path.Combine(_root, Key) + ".tmp"));   // 임시 파일이 남지 않는다
    }

    [Fact]
    public async Task Delete_RemovesFile_AndIsIdempotent()
    {
        await _storage.PutAsync(Key, new MemoryStream(Bytes), "image/webp", CancellationToken.None);

        await _storage.DeleteAsync(Key, CancellationToken.None);
        await _storage.DeleteAsync(Key, CancellationToken.None);

        Assert.Null(await _storage.OpenAsync(Key, CancellationToken.None));
    }

    [Theory]
    [InlineData("../outside.webp")]
    [InlineData("reviews/2026/10/27/../../../../outside.webp")]
    public async Task Put_WithKeyNotOurShape_Throws(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.PutAsync(key, new MemoryStream(Bytes), "image/webp", CancellationToken.None));
        Assert.False(File.Exists(Path.GetFullPath(Path.Combine(_root, key))));
    }

    [Theory]
    [InlineData("../outside.webp")]
    [InlineData("reviews/2026/10/27/0123456789abcdef0123456789abcdef.exe")]
    public async Task Open_WithKeyNotOurShape_ReturnsNull(string key)
    {
        Assert.Null(await _storage.OpenAsync(key, CancellationToken.None));
    }
}
