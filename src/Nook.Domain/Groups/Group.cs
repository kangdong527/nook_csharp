using Nook.Domain.Common;
using Nook.SharedKernel;
using Nook.Domain.Users;

namespace Nook.Domain.Groups;

// 그룹 애그리거트 루트. GroupName·InviteCode(값)와 GroupMember(내부 엔티티)를 포함하고 User는 ID로만 참조한다.
// 지키는 규칙: R-GR-01(정원), R-GR-07(중복 합류 — 정원보다 먼저 검사), R-GR-08(만든 회원이 첫 멤버), R-GR-09(마지막 멤버가 나가면 GroupEmptied).
// 루트의 검사는 인스턴스 하나 안의 미리 검사다. 동시 합류·탈퇴의 최종 보장은 그룹 행 잠금과 DB 제약(D6), R-GR-02(회원당 그룹 수)는 Application.
// 방장(R-GR-11, FE-2 PM 허가 — ADR-004 결정 6 번복, ADR-012): 만든 회원. 방장만 이름을 바꾼다(GR-03).
// 방장이 나가면 남은 멤버 중 가장 먼저 합류한 회원에게 넘어간다 — 원본의 owner_id CASCADE(방장 탈퇴 = 그룹 삭제)를 되풀이하지 않는다.
// 색 슬롯(R-GR-12): 멤버마다 그룹 안에서 고유한 0~3, 합류 때 안 쓰인 슬롯 중 랜덤(PM). 색 값은 디자인 확정 뒤 FE가 슬롯 → 색으로.
public sealed class Group
{
    public const int MaxGroupMembers = 4;

    // R-GR-02. 여러 그룹에 걸친 규칙이라 루트가 검사하지 못한다 — 값만 여기 두고 Application이 users 행 잠금 뒤 센다(R-GR-04).
    public const int MaxGroupsPerUser = 3;

    // R-GR-12. 정원과 같다 — 멤버마다 서로 다른 슬롯이 반드시 남는다.
    public const int ColorSlotCount = MaxGroupMembers;

    public GroupId Id { get; }
    public GroupName Name { get; private set; }
    public InviteCode InviteCode { get; }
    public UserId OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private readonly List<GroupMember> _members = [];

    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    private Group(GroupId id, GroupName name, InviteCode inviteCode, UserId ownerId, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        InviteCode = inviteCode;
        OwnerId = ownerId;
        CreatedAt = createdAt;
    }

    // pickSlot: 0 이상 maxExclusive 미만을 돌려주는 함수(InviteCode.Generate와 같은 모양). 테스트는 고정 함수를 넘긴다.
    // 추측 불가가 필요 없는 값(색)이라 기본은 Random.Shared — 초대 코드(RandomNumberGenerator)와 다르다.
    public static Group Create(
       GroupName groupName,
       UserId creatorId,
       InviteCode inviteCode,
       DateTimeOffset createdAt,
       Func<int, int>? pickSlot = null)
    {
        // 이미 검증된 값 객체·ID가 null이면 호출 코드의 실수 → 예외. creatorId는 GroupMember.Create가 아니라 여기서 먼저 막는다.
        ArgumentNullException.ThrowIfNull(groupName);
        ArgumentNullException.ThrowIfNull(creatorId);
        ArgumentNullException.ThrowIfNull(inviteCode);

        var group = new Group(GroupId.New(), groupName, inviteCode, creatorId, TimeGuard.Utc(createdAt));
        group._members.Add(GroupMember.Create(creatorId, createdAt, group.PickFreeSlot(pickSlot)));

        return group;
    }

    public Result AddMember(UserId userId, DateTimeOffset joinedAt, Func<int, int>? pickSlot = null)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // 시각 검사를 Result 실패보다 먼저: 호출 코드의 실수는 정원·중복 여부와 상관없이 드러나야 한다(Review.Create와 같은 순서).
        // 그룹이 생기기 전 합류는 시각을 두 번 정한 호출 코드의 실수다(D4 결정). 같은 시각은 허용 — 만든 회원도 CreatedAt에 합류한다.
        var utcJoinedAt = TimeGuard.Utc(joinedAt);
        ArgumentOutOfRangeException.ThrowIfLessThan(utcJoinedAt, CreatedAt, nameof(joinedAt));

        // R-GR-09: 멤버 0명인 그룹은 존재하지 않는다. GroupEmptied 뒤 Application이 삭제해야 할 그룹이므로
        // 여기에 합류시키는 것은 회원 입력이 아니라 호출 코드의 실수다.
        if (_members.Count == 0)
            throw new InvalidOperationException("멤버가 없는 그룹(GroupEmptied)에는 합류할 수 없습니다.");

        if (_members.Any(m => m.UserId == userId))
            return GroupErrors.AlreadyMember;      // R-GR-07: 중복을 정원보다 먼저

        if (_members.Count >= MaxGroupMembers)
            return GroupErrors.GroupFull;          // R-GR-01

        // 동시 합류: Application이 그룹 행을 잠근 뒤 멤버를 읽으므로(D6) 같은 슬롯을 두 번 고르지 않는다. DB UNIQUE가 이중화.
        _members.Add(GroupMember.Create(userId, utcJoinedAt, PickFreeSlot(pickSlot)));
        return Result.Success();
    }

    // GR-03. 방장만(R-GR-11). 같은 이름이면 아무것도 바꾸지 않는다(멱등).
    public Result Rename(GroupName newName, UserId requestedBy)
    {
        ArgumentNullException.ThrowIfNull(newName);
        ArgumentNullException.ThrowIfNull(requestedBy);

        if (requestedBy != OwnerId)
            return GroupErrors.NotOwner;

        Name = newName;
        return Result.Success();
    }

    private short PickFreeSlot(Func<int, int>? pickSlot)
    {
        var free = Enumerable.Range(0, ColorSlotCount).Where(s => _members.All(m => m.ColorSlot != s)).ToList();
        var index = (pickSlot ?? Random.Shared.Next)(free.Count);
        return (short)free[index];
    }

    // 멱등: 멤버가 아니면 아무것도 바꾸지 않고 NotMember. 방금 마지막 멤버가 나갔으면 GroupEmptied(R-GR-09).
    public MemberRemoval RemoveMember(UserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var member = _members.Find(m => m.UserId == userId);
        if (member is null)
            return MemberRemoval.NotMember;

        _members.Remove(member);
        if (_members.Count == 0)
            return MemberRemoval.GroupEmptied;

        // R-GR-11: 방장이 나가면 가장 먼저 합류한 회원에게. 같은 시각이면 UserId로 동점을 깬다(결정적).
        if (member.UserId == OwnerId)
            OwnerId = _members.OrderBy(m => m.JoinedAt).ThenBy(m => m.UserId.Value).First().UserId;

        return MemberRemoval.Removed;
    }
}
