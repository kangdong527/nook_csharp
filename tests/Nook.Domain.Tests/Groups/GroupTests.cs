using Nook.Domain.Groups;
using Nook.Domain.Users;

namespace Nook.Domain.Tests.Groups;

// Group 애그리거트 루트 단위 테스트 (D2 TDD 시범).
// 규칙: R-GR-01(정원), R-GR-07(중복, 정원보다 먼저), R-GR-08(만든 회원이 첫 멤버), R-GR-09(마지막 멤버 → GroupEmptied, 멱등)
public class GroupTests
{
    // 테스트 목록
    //
    // Create (R-GR-08)
    // - Create_WithCreator_CreatorIsOnlyMember
    // - Create_WithCreator_CreatorJoinedAtIsCreatedAt
    //
    // AddMember (R-GR-01, R-GR-07)
    // - AddMember_WhenNewUser_AddsMember
    // - AddMember_WhenFourthMember_Succeeds                  (만든 회원 1 + AddMember 3번째 호출)
    // - AddMember_WhenGroupIsFull_ReturnsGroupFull           (5번째 멤버 = AddMember 4번째 호출)
    // - AddMember_WhenAlreadyMember_ReturnsAlreadyMember
    // - AddMember_WhenCreatorJoinsAgain_ReturnsAlreadyMember
    // - AddMember_WhenFullAndAlreadyMember_ReturnsAlreadyMember   (중복이 정원보다 먼저)
    // - AddMember_WhenGroupIsFull_DoesNotChangeMembers
    // - AddMember_WhenSeatFreedByRemoval_Succeeds            (가득 찬 그룹에서 1명 탈퇴 후 합류)
    //
    // RemoveMember (R-GR-09, 멱등, MemberRemoval 3상태)
    // - RemoveMember_WhenMember_ReturnsRemoved
    // - RemoveMember_WhenMember_RemovesFromMembers
    // - RemoveMember_WhenNotMember_ReturnsNotMember
    // - RemoveMember_WhenNotMember_DoesNotChangeMembers
    // - RemoveMember_WhenSameMemberTwice_SecondReturnsNotMember
    // - RemoveMember_WhenLastMember_ReturnsGroupEmptied
    // - RemoveMember_WhenLastMember_MembersIsEmpty

    private static readonly DateTimeOffset Now = new(2026, 10, 13, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithCreator_CreatorIsOnlyMember()
    {
        var creator = UserId.New();

        var group = Group.Create(ValidName(), creator, InviteCode.Generate(_ => 0),  Now);

        var member = Assert.Single(group.Members);
        Assert.Equal(creator, member.UserId);
    }

    [Fact]
    public void AddMember_WhenNewUser_AddsMember()
    {
        var group = NewGroup();
        var newUser = UserId.New();

        var result = group.AddMember(newUser, Now.AddMinutes(1));

        Assert.False(result.IsFailure(out var error), error?.Code);
        Assert.Equal(2, group.Members.Count);
        Assert.Contains(group.Members, m => m.UserId == newUser);
    }

    [Fact]
    public void AddMember_WhenAlreadyMember_ReturnsAlreadyMember()
    {
        var group = NewGroup();
        var member = UserId.New();
        group.AddMember(member, Now.AddMinutes(1));

        var result = group.AddMember(member, Now.AddMinutes(2));

        Assert.True(result.IsFailure(out var error));
        Assert.Equal(GroupErrors.AlreadyMember.Code, error.Code);
    }

    [Fact]
    public void AddMember_WhenGroupIsFull_ReturnsGroupFull()
    {
        var group = NewGroup();                                // 1번째 멤버: 만든 회원
        group.AddMember(UserId.New(), Now.AddMinutes(1));      // 2번째
        group.AddMember(UserId.New(), Now.AddMinutes(2));      // 3번째
        group.AddMember(UserId.New(), Now.AddMinutes(3));      // 4번째 — 정원 도달

        var result = group.AddMember(UserId.New(), Now.AddMinutes(4));   // 5번째

        Assert.True(result.IsFailure(out var error));
        Assert.Equal(GroupErrors.GroupFull.Code, error.Code);
    }

    [Fact]
    public void AddMember_WhenFourthMember_Succeeds()
    {
        var group = NewGroup();                                // 1번째 멤버: 만든 회원
        group.AddMember(UserId.New(), Now.AddMinutes(1));      // 2번째
        group.AddMember(UserId.New(), Now.AddMinutes(2));      // 3번째

        var result = group.AddMember(UserId.New(), Now.AddMinutes(3));   // 4번째 — 정원 경계

        Assert.False(result.IsFailure(out var error), error?.Code);
        Assert.Equal(4, group.Members.Count);
    }

    [Fact]
    public void AddMember_WhenFullAndAlreadyMember_ReturnsAlreadyMember()
    {
        var group = NewGroup();
        var member = UserId.New();
        group.AddMember(member, Now.AddMinutes(1));
        group.AddMember(UserId.New(), Now.AddMinutes(2));
        group.AddMember(UserId.New(), Now.AddMinutes(3));      // 정원 도달

        var result = group.AddMember(member, Now.AddMinutes(4));   // 가득 찼고, 이미 멤버

        Assert.True(result.IsFailure(out var error));
        Assert.Equal(GroupErrors.AlreadyMember.Code, error.Code);
    }

    [Fact]
    public void AddMember_WhenGroupIsFull_DoesNotChangeMembers()
    {
        var group = NewGroup();
        group.AddMember(UserId.New(), Now.AddMinutes(1));
        group.AddMember(UserId.New(), Now.AddMinutes(2));
        group.AddMember(UserId.New(), Now.AddMinutes(3));      // 정원 도달
        var before = group.Members.Select(m => m.UserId).ToList();
        var newcomer = UserId.New();

        group.AddMember(newcomer, Now.AddMinutes(4));          // GroupFull로 실패

        Assert.Equal(before, group.Members.Select(m => m.UserId));
        Assert.DoesNotContain(group.Members, m => m.UserId == newcomer);
    }

    [Fact]
    public void Create_WithCreator_CreatorJoinedAtIsCreatedAt()
    {
        var createdAt = Now.AddHours(1);
        var group = Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), createdAt);

        var member = Assert.Single(group.Members);
        Assert.Equal(createdAt, member.JoinedAt);
    }

    [Fact]
    public void AddMember_WhenCreatorJoinsAgain_ReturnsAlreadyMember()
    {
        var creator = UserId.New();
        var group = Group.Create(ValidName(), creator, InviteCode.Generate(_ => 0), Now);

        var result = group.AddMember(creator, Now.AddMinutes(1));

        Assert.True(result.IsFailure(out var error));
        Assert.Equal(GroupErrors.AlreadyMember.Code, error.Code);
    }

    private static Group NewGroup() =>
        Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), Now);

    private static GroupName ValidName()
    {
        Assert.True(GroupName.Create("주말 산책").TryGetValue(out var name, out _));
        return name;
    }
}

