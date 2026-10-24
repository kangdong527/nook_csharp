namespace Nook.Application.Reviews;

// 리뷰 이미지 스토리지(외부 I/O). 트랜잭션 밖에서만 부른다(CLAUDE.md). 키는 호출 쪽이 ReviewImageKeys로 만든다.
// D5는 메모리 구현, 실제 스토리지(S3)는 DEP 주간(ADR-009).
public interface IReviewImageStorage
{
    Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct);

    // RV-02·RV-03: DB 커밋 후에만 부른다. 실패해도 배치가 잡는다(erd.md §3).
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
