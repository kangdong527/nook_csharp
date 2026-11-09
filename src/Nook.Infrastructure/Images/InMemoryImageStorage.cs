using System.Collections.Concurrent;
using Nook.Application.Images;

namespace Nook.Infrastructure.Images;

// 개발·테스트 전용 스토리지(D5). 실제 스토리지(S3)는 DEP 주간에 붙인다(ADR-009).
// 프로세스 메모리라 재시작하면 사라진다. 테스트가 "무엇이 올라갔는가"를 확인할 수 있게 Objects를 공개한다.
public sealed class InMemoryImageStorage : IImageStorage
{
    private readonly ConcurrentDictionary<string, StoredImage> _objects = new();

    public IReadOnlyDictionary<string, StoredImage> Objects => _objects;

    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        _objects[storageKey] = new StoredImage(buffer.ToArray(), contentType);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        _objects.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }

    public Task<StoredImageStream?> OpenAsync(string storageKey, CancellationToken ct) =>
        Task.FromResult(_objects.TryGetValue(storageKey, out var image)
            ? new StoredImageStream(new MemoryStream(image.Bytes, writable: false), image.ContentType)
            : null);
}

public sealed record StoredImage(byte[] Bytes, string ContentType);
