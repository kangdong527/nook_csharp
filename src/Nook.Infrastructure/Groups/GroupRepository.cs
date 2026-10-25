using Microsoft.EntityFrameworkCore;
using Nook.Application.Groups;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;
using Npgsql;

namespace Nook.Infrastructure.Groups;

internal sealed class GroupRepository(NookDbContext db) : IGroupRepository
{
    public async Task<Group?> LockByInviteCodeAsync(InviteCode inviteCode, CancellationToken ct)
    {
        // 문장 1 — 잠금만. erd.md groups Q1·Q2. 받는 인덱스: ux_groups_invite_code.
        // 잠금과 멤버 로딩을 한 문장으로 합치면 안 된다: EF가 FromSql을 서브쿼리로 감싸 group_members를 JOIN하는데,
        // 잠금 대기 뒤 PostgreSQL이 최신으로 다시 보는 것은 잠긴 groups 행뿐이고 JOIN한 멤버는 문장 시작 스냅샷이다(D6 실험 1: 5명 모두 성공, 6명).
        // ToList인 이유: SingleOrDefaultAsync는 SQL을 서브쿼리로 감싼다(PlaceRepository.TryAddAsync와 같은 이유).
        var locked = await db.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM groups WHERE invite_code = {inviteCode.Value} FOR UPDATE
            """).ToListAsync(ct);

        if (locked is [])
            return null;

        // 2개 이상 = ux_groups_invite_code가 깨진 데이터 손상. 회원 입력 문제가 아니므로 예외(InviteCode.From과 같은 기준).
        if (locked is not [var id])
            throw new InvalidOperationException(
                $"초대 코드 {inviteCode.Value}에 해당하는 그룹이 {locked.Count}개입니다(ux_groups_invite_code 위반).");

        // 문장 2 — 새 문장 = 새 스냅샷(READ COMMITTED). 잠금을 얻기 전에 커밋된 멤버가 모두 보인다.
        // erd.md groups Q3 + group_members Q1. 받는 인덱스: pk_groups, pk_group_members. 추적한다 — SaveAsync가 새 멤버를 INSERT한다.
        var groupId = GroupId.From(id);
        return await db.Groups.SingleAsync(g => g.Id == groupId, ct);
    }

    public async Task<bool> TryAddAsync(Group group, CancellationToken ct)
    {
        // erd.md groups Q4 + group_members Q3. 충돌 판정: ux_groups_invite_code.
        // groups·group_members INSERT가 SaveChanges 한 번 = 암묵적 트랜잭션 하나 — 멤버 INSERT가 실패하면 그룹도 남지 않는다(GR-02 원자성).
        db.Groups.Add(group);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_groups_invite_code",
        })
        {
            // 실패한 Added 상태(그룹 + owned 멤버)가 남으면 다음 SaveChanges가 같은 INSERT를 다시 보낸다(ReviewRepository와 같은 이유).
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public Task<int> CountMembershipsAsync(UserId userId, CancellationToken ct) =>
        // erd.md group_members Q2. 받는 인덱스: ix_group_members_user_id. 멤버는 Group의 owned 타입이라 DbSet이 없다 → SQL.
        db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::int AS "Value" FROM group_members WHERE user_id = {userId.Value}
            """).SingleAsync(ct);

    public Task SaveAsync(Group group, CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
