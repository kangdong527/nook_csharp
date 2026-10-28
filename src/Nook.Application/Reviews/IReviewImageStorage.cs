namespace Nook.Application.Reviews;

// 리뷰 이미지 스토리지(외부 I/O). 트랜잭션 밖에서만 부른다(CLAUDE.md). 키는 호출 쪽이 ReviewImageKeys로 만든다.
// D5는 메모리 구현, 실제 스토리지(S3)는 DEP 주간(ADR-009).
public interface IReviewImageStorage
{
    Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct);

    // RV-02·RV-03: DB 커밋 후에만 부른다. 실패해도 배치가 잡는다(erd.md §3).
    Task DeleteAsync(string storageKey, CancellationToken ct);

    // GET /images/{key}(FE-2 이미지 A안). 없으면 null. 키 형식은 호출 쪽이 ReviewImageKeys.IsValid로 먼저 거른다.
    Task<StoredImageStream?> OpenAsync(string storageKey, CancellationToken ct);
}

// 읽은 이미지. Content는 받은 쪽이 닫는다(응답 스트림으로 넘기면 프레임워크가 닫는다).
public sealed record StoredImageStream(Stream Content, string ContentType);
