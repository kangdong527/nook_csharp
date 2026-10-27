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

        return await LoadLockedAsync(GroupId.From(id), ct);
    }

    public async Task<Group?> LockByIdAsync(GroupId groupId, CancellationToken ct)
    {
        // 문장 1 — 잠금만. erd.md groups Q2(탈퇴). 받는 인덱스: pk_groups. 나누는 이유는 LockByInviteCodeAsync와 같다.
        // 다른 트랜잭션이 이 그룹을 지우고 커밋하면, 기다리던 이 문장은 지워진 행을 건너뛰어 빈 결과가 된다.
        var locked = await db.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM groups WHERE id = {groupId.Value} FOR UPDATE
            """).ToListAsync(ct);

        return locked is [] ? null : await LoadLockedAsync(groupId, ct);
    }

    // 문장 2 — 새 문장 = 새 스냅샷(READ COMMITTED). 잠금을 얻기 전에 커밋된 멤버가 모두 보인다.
    // erd.md groups Q3 + group_members Q1. 받는 인덱스: pk_groups, pk_group_members. 추적한다 — SaveAsync가 멤버 변경을 저장한다.
    private Task<Group> LoadLockedAsync(GroupId groupId, CancellationToken ct) =>
        db.Groups.SingleAsync(g => g.Id == groupId, ct);

    public Task DeleteAsync(Group group, CancellationToken ct)
    {
        // erd.md groups Q5. R-GR-09로 멤버 0명일 때만 불린다 — 마지막 멤버 DELETE와 그룹 DELETE가 SaveChanges 한 번에 나간다.
        db.Groups.Remove(group);
        return db.SaveChangesAsync(ct);
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

    public async Task<IReadOnlyList<MyGroupView>> ListMineAsync(UserId userId, CancellationToken ct)
    {
        // 한 문장: 내 멤버십이 있는 그룹 + 멤버 수·내 슬롯·합류 시각을 상관 하위 쿼리로(행 수 ≤ 3, R-GR-02).
        // 받는 인덱스(예상): ix_group_members_user_id(내 그룹 찾기), pk_group_members(group_id 앞 — 멤버 수). D9에서 EXPLAIN.
        var rows = await db.Groups
            .AsNoTracking()
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.InviteCode,
                MemberCount = g.Members.Count,
                Mine = g.Members.Where(m => m.UserId == userId).Select(m => new { m.ColorSlot, m.JoinedAt }).First(),
                IsOwner = g.OwnerId == userId,
            })
            .ToListAsync(ct);

        return [.. rows
            .OrderBy(r => r.Mine.JoinedAt)
            .Select(r => new MyGroupView(r.Id.Value, r.Name.Value, r.InviteCode.Value, r.MemberCount, r.Mine.ColorSlot, r.IsOwner))];
    }

    public async Task<bool?> IsMemberAsync(GroupId groupId, UserId userId, CancellationToken ct)
    {
        // 한 문장: 그룹 존재 + 멤버 여부. 받는 인덱스: pk_groups, pk_group_members.
        var row = await db.Groups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { IsMember = g.Members.Any(m => m.UserId == userId) })
            .SingleOrDefaultAsync(ct);
        return row?.IsMember;
    }

    public async Task<IReadOnlyList<GroupPlaceView>> ListPlacesAsync(GroupId groupId, CancellationToken ct) =>
        // 현재 멤버(m)만 모은다 — 나간 멤버·탈퇴 회원(reviews.user_id가 PF-02 뒤 NULL)의 기록은 JOIN에서 빠진다.
        // 첫 리뷰어: 현재 멤버의 리뷰 중 created_at(동점은 id)이 가장 이른 것의 슬롯 — 저장하지 않고 매번 계산(R-STAR와 같은 이유).
        // 받는 인덱스(예상): pk_group_members(group_id 앞), reviews·bookmarks의 user_id 앞 인덱스. 정렬 없음(지도라 순서 무관). D9에서 EXPLAIN.
        await db.Database.SqlQuery<GroupPlaceView>($"""
            WITH m AS (
                SELECT user_id, color_slot FROM group_members WHERE group_id = {groupId.Value}
            ),
            rv AS (
                SELECT r.place_id,
                       COUNT(DISTINCT r.user_id)::int AS review_members,
                       (array_agg(m.color_slot ORDER BY r.created_at, r.id))[1]::int AS first_slot
                FROM reviews r JOIN m ON m.user_id = r.user_id
                GROUP BY r.place_id
            ),
            bm AS (
                SELECT b.place_id, COUNT(*)::int AS bookmarks, MIN(m.color_slot)::int AS any_slot
                FROM bookmarks b JOIN m ON m.user_id = b.user_id
                GROUP BY b.place_id
            )
            SELECT p.id AS "PlaceId", p.kakao_place_id AS "KakaoPlaceId", p.name AS "Name", p.address AS "Address",
                   p.latitude AS "Latitude", p.longitude AS "Longitude",
                   COALESCE(rv.review_members, 0) AS "ReviewMemberCount", rv.first_slot AS "FirstReviewerSlot",
                   COALESCE(bm.bookmarks, 0) AS "BookmarkCount",
                   CASE WHEN bm.bookmarks = 1 THEN bm.any_slot END AS "SoleBookmarkerSlot"
            FROM places p
            LEFT JOIN rv ON rv.place_id = p.id
            LEFT JOIN bm ON bm.place_id = p.id
            WHERE rv.place_id IS NOT NULL OR bm.place_id IS NOT NULL
            """).ToListAsync(ct);
}
