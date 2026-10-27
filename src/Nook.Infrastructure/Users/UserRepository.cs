using Microsoft.EntityFrameworkCore;
using Nook.Application.Users;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;
using Nook.SharedKernel;
using Npgsql;

namespace Nook.Infrastructure.Users;

internal sealed class UserRepository(NookDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(Email email, CancellationToken ct) =>
        // erd.md users Q4. 받는 인덱스: ux_users_email_lower. LINQ로 쓰면 값 변환기 때문에 표기 그대로 비교된다 → SQL로 lower().
        db.Users
            .FromSql($"SELECT * FROM users WHERE lower(email) = lower({email.Value})")
            .SingleOrDefaultAsync(ct);

    public Task<User?> FindByIdAsync(UserId userId, CancellationToken ct) =>
        // erd.md users Q1. 받는 인덱스: pk_users.
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct);

    public Task<User?> FindForUpdateAsync(UserId userId, CancellationToken ct) =>
        // erd.md users Q1. 추적 쿼리 — 핸들러가 ChangeNickname으로 바꾼 것을 SaveAsync가 그대로 저장한다.
        db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);

    public async Task<Result> AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        return await SaveNicknameGuardedAsync(ct);
        // ux_users_email_lower 위반(같은 이메일 동시 가입)은 일부러 잡지 않는다 → 500. 처리 방식은 미결(progress.md D5).
    }

    public Task<Result> SaveAsync(User user, CancellationToken ct) => SaveNicknameGuardedAsync(ct);

    // 닉네임 UNIQUE 위반만 예상 가능한 실패로 바꾼다(R-US-02 — 동시 가입·동시 변경). 위반이 나면 이 DbContext는 더 쓰지 않는다.
    private async Task<Result> SaveNicknameGuardedAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_users_nickname_lower",
        })
        {
            return UserErrors.NicknameTaken;
        }
    }

    public Task LockAsync(UserId userId, CancellationToken ct) =>
        // erd.md users Q2. 받는 인덱스: pk_users. 엔티티를 싣지 않는다 — 잠금만 필요하다.
        db.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM users WHERE id = {userId.Value} FOR NO KEY UPDATE
            """).ToListAsync(ct);
}
