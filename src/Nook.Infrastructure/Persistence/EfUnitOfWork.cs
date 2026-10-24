using Microsoft.EntityFrameworkCore.Storage;
using Nook.Application.Common;

namespace Nook.Infrastructure.Persistence;

// 요청당 DbContext 하나(Scoped)의 트랜잭션을 연다. 같은 요청의 저장소들이 같은 DbContext를 받으므로
// 저장소 안의 SaveChanges·raw SQL이 모두 이 트랜잭션 안에서 실행된다. 격리 수준은 기본값(READ COMMITTED).
internal sealed class EfUnitOfWork(NookDbContext db) : IUnitOfWork
{
    public async Task<ITransaction> BeginAsync(CancellationToken ct) =>
        new EfTransaction(await db.Database.BeginTransactionAsync(ct));

    private sealed class EfTransaction(IDbContextTransaction inner) : ITransaction
    {
        public Task CommitAsync(CancellationToken ct) => inner.CommitAsync(ct);

        // 커밋하지 않은 트랜잭션의 Dispose = 롤백(EF Core 동작)
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
