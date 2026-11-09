namespace Nook.Application.Images;

// 이미지 스토리지(외부 I/O) — 리뷰 이미지(reviews/)와 프로필 사진(avatars/)이 함께 쓴다.
// 트랜잭션 밖에서만 부른다(CLAUDE.md). 키는 호출 쪽이 ReviewImageKeys로 만든다.
// 구현: 메모리(테스트)·로컬 디스크(개발)·S3(운영, DEP). 선택은 Program.cs의 ImageStorage:Provider(ADR-003 결정 1).
public interface IImageStorage
{
    Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct);

    // RV-02·RV-03: DB 커밋 후에만 부른다. 실패해도 배치가 잡는다(erd.md §3).
    Task DeleteAsync(string storageKey, CancellationToken ct);

    // GET /images/{key}(FE-2 이미지 A안). 없으면 null. 키 형식은 호출 쪽이 ReviewImageKeys.IsValid로 먼저 거른다.
    Task<StoredImageStream?> OpenAsync(string storageKey, CancellationToken ct);
}

// 읽은 이미지. Content는 받은 쪽이 닫는다(응답 스트림으로 넘기면 프레임워크가 닫는다).
public sealed record StoredImageStream(Stream Content, string ContentType);
