using Nook.Domain.Common;
using Nook.Domain.Users;

namespace Nook.Domain.Groups;

// 그룹 애그리거트 루트. GroupName·InviteCode(값)와 GroupMember(내부 엔티티)를 포함하고 User는 ID로만 참조한다.
// 지키는 규칙: R-GR-01(정원), R-GR-07(중복 합류 — 정원보다 먼저 검사), R-GR-08(만든 회원이 첫 멤버), R-GR-09(마지막 멤버가 나가면 GroupEmptied).
// 루트의 검사는 인스턴스 하나 안의 미리 검사다. 동시 합류·탈퇴의 최종 보장은 그룹 행 잠금과 DB 제약(D6), R-GR-02(회원당 그룹 수)는 Application.
// 방장 없음: 방장을 읽는 유스케이스가 없다(ADR-004).
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
        // 이미 검증된 값 객체·ID가 null이면 호출 코드의 실수 → 예외. creatorId는 GroupMember.Create가 아니라 여기서 먼저 막는다.
        ArgumentNullException.ThrowIfNull(groupName);
        ArgumentNullException.ThrowIfNull(creatorId);
        ArgumentNullException.ThrowIfNull(inviteCode);

        var group = new Group(GroupId.New(), groupName, inviteCode, TimeGuard.Utc(createdAt));
        group._members.Add(GroupMember.Create(creatorId, createdAt));

        return group;
    }

    public Result AddMember(UserId userId, DateTimeOffset joinedAt)
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

        _members.Add(GroupMember.Create(userId, utcJoinedAt));
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
