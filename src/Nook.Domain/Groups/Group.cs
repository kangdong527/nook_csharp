
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
}
