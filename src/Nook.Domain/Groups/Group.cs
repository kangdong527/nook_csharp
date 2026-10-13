
using Nook.Domain.Common;
using Nook.Domain.Users;

namespace Nook.Domain.Groups;

public sealed class Group
{
    public const int MaxGroupMembers = 4;

    public GroupId Id { get; }
    public GroupName Name { get; }
    public InviteCode InviteCode { get; }
    public DateTimeOffset CreatedAt { get; }

    private readonly List<GroupMember> _members = [];

    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    private Group(GroupId id, GroupName name, InviteCode inviteCode, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        InviteCode = inviteCode;
        CreatedAt = createdAt;
    }

    public static Group Create(
       GroupName groupName,
       UserId creatorId,
       InviteCode inviteCode,
       DateTimeOffset createdAt)
    {
        var group = new Group(GroupId.New(), groupName, inviteCode, TimeGuard.Utc(createdAt));
        group._members.Add(GroupMember.Create(creatorId, createdAt));

        return group;
    }

    public Result AddMember(UserId userId, DateTimeOffset joinedAt)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // R-GR-09: 멤버 0명인 그룹은 존재하지 않는다. GroupEmptied 뒤 Application이 삭제해야 할 그룹이므로
        // 여기에 합류시키는 것은 회원 입력이 아니라 호출 코드의 실수다.
        if (_members.Count == 0)
            throw new InvalidOperationException("멤버가 없는 그룹(GroupEmptied)에는 합류할 수 없습니다.");

        if (_members.Any(m => m.UserId == userId))
            return GroupErrors.AlreadyMember;      // R-GR-07: 중복을 정원보다 먼저

        if (_members.Count >= MaxGroupMembers)
            return GroupErrors.GroupFull;          // R-GR-01

        _members.Add(GroupMember.Create(userId, joinedAt));
        return Result.Success();
    }

    // 멱등: 멤버가 아니면 아무것도 바꾸지 않고 NotMember. 방금 마지막 멤버가 나갔으면 GroupEmptied(R-GR-09).
    public MemberRemoval RemoveMember(UserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var member = _members.Find(m => m.UserId == userId);
        if (member is null)
            return MemberRemoval.NotMember;

        _members.Remove(member);

        return _members.Count == 0
            ? MemberRemoval.GroupEmptied
            : MemberRemoval.Removed;
    }
}
