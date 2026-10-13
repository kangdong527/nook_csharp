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
    // - AddMember_WhenFails_DoesNotChangeMembers
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

    private static GroupName ValidName()
    {
        Assert.True(GroupName.Create("주말 산책").TryGetValue(out var name, out _));
        return name;
    }
}
