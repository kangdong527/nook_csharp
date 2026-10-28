using Nook.Application.Reviews;

namespace Nook.Infrastructure.Reviews;

// 개발용 로컬 디스크 스토리지(FE-2 이미지 A안). 재시작해도 남는다 — 화면 확인용. 운영(S3)은 DEP 주간에 구현체만 교체한다.
// 키 → 경로: {root}/{key}. 키 형식은 ReviewImageKeys가 정한 모양만 받는다(경로 조작 방어 1차),
// 합친 경로가 root 밖이면 거절한다(2차 — 형식 규칙이 나중에 느슨해져도 디스크 밖으로 나가지 않게).
public sealed class LocalDiskReviewImageStorage(string root) : IReviewImageStorage
{
    private readonly string _root = Path.GetFullPath(root);

    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct)
    {
        // 우리가 만든 키만 들어온다 — 어긋나면 호출 경로 결함이라 예외.
        var path = PathOf(storageKey) ?? throw new ArgumentException($"저장소 키 형식이 아닙니다: {storageKey}", nameof(storageKey));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // 임시 파일에 다 쓴 뒤 옮긴다 — 쓰다 실패한 반쪽 파일이 키 이름으로 보이지 않게.
        var temp = path + ".tmp";
        await using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await content.CopyToAsync(file, ct);
        File.Move(temp, path, overwrite: true);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        if (PathOf(storageKey) is { } path)
            File.Delete(path);   // 없으면 아무 일도 없다(멱등)
        return Task.CompletedTask;
    }

    public Task<StoredImageStream?> OpenAsync(string storageKey, CancellationToken ct)
    {
        if (PathOf(storageKey) is not { } path || !File.Exists(path))
            return Task.FromResult<StoredImageStream?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<StoredImageStream?>(new StoredImageStream(stream, ReviewImageKeys.ContentTypeOf(storageKey)));
    }

    private string? PathOf(string storageKey)
    {
        if (!ReviewImageKeys.IsValid(storageKey))
            return null;

        var path = Path.GetFullPath(Path.Combine(_root, storageKey));
        return path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? path : null;
    }
}
