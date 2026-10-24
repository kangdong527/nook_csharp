namespace Nook.Application.Common;

// 트랜잭션 경계는 유스케이스(핸들러)가 쥔다(D5-3 사용자 결정 (가)). 어떤 판단을 한 트랜잭션에 묶을지는 업무 규칙이기 때문이다.
// 사용: await using var tx = await uow.BeginAsync(ct); ... await tx.CommitAsync(ct);
// CommitAsync 없이 Dispose되면 롤백된다 — 중간에 return한 실패 경로는 따로 롤백을 부르지 않아도 된다.
// 안에서 외부 I/O(메일·이미지 업로드·외부 API)를 부르지 않는다(CLAUDE.md).
public interface IUnitOfWork
{
    Task<ITransaction> BeginAsync(CancellationToken ct);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
