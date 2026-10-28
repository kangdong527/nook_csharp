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
    // - Create_WhenNameIsNull_Throws
    // - Create_WhenInviteCodeIsNull_Throws
    // - Create_WhenCreatedAtIsInvalid_ThrowsWithParamName      (ADR-007: default, 비UTC — Group.cs의 가드)
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
    // - AddMember_WhenGroupEmptied_Throws
    // - AddMember_WhenJoinedAtIsInvalid_ThrowsWithParamName    (ADR-007: default, 비UTC — GroupMember.cs의 가드)
    // - AddMember_WhenJoinedAtIsBeforeCreatedAt_Throws       (D4 결정: 그룹이 생기기 전에 합류할 수 없다)
    // - AddMember_WhenJoinedAtEqualsCreatedAt_Succeeds       (경계: 같은 시각은 허용 — 만든 회원도 CreatedAt에 합류)
    //
    // RemoveMember (R-GR-09, 멱등, MemberRemoval 3상태)
    // - RemoveMember_WhenMember_ReturnsRemoved
    // - RemoveMember_WhenMember_RemovesFromMembers
    // - RemoveMember_WhenNotMember_ReturnsNotMember
    // - RemoveMember_WhenNotMember_DoesNotChangeMembers
    // - RemoveMember_WhenSameMemberTwice_SecondReturnsNotMember
    // - RemoveMember_WhenLastMember_ReturnsGroupEmptied
    // - RemoveMember_WhenLastMember_MembersIsEmpty
    //
    // 방장(R-GR-11)·색 슬롯(R-GR-12)·이름 변경(GR-03) — FE-2
    // - Create_CreatorIsOwnerWithPickedSlot
    // - AddMember_PicksOnlyFreeSlots_AllFourDistinct          (고정 함수 n => 0 → 매번 남은 것 중 가장 작은 슬롯)
    // - AddMember_AfterRemoval_ReusesFreedSlot
    // - RemoveMember_WhenOwnerLeaves_OwnershipGoesToEarliestJoined
    // - RemoveMember_WhenOtherLeaves_OwnerUnchanged
    // - Rename_ByOwner_ChangesName / Rename_ByMember_ReturnsNotOwner

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

    [Fact]
    public void RemoveMember_WhenMember_ReturnsRemoved()
    {
        var group = NewGroup();
        var member = UserId.New();
        group.AddMember(member, Now.AddMinutes(1));

        var result = group.RemoveMember(member);

        Assert.Equal(MemberRemoval.Removed, result);
    }

    [Fact]
    public void RemoveMember_WhenMember_RemovesFromMembers()
    {
        var group = NewGroup();
        var member = UserId.New();
        group.AddMember(member, Now.AddMinutes(1));

        group.RemoveMember(member);

        Assert.Single(group.Members);
        Assert.DoesNotContain(group.Members, m => m.UserId == member);
    }

    [Fact]
    public void RemoveMember_WhenNotMember_ReturnsNotMember()
    {
        var group = NewGroup();

        var result = group.RemoveMember(UserId.New());

        Assert.Equal(MemberRemoval.NotMember, result);
    }

    [Fact]
    public void RemoveMember_WhenNotMember_DoesNotChangeMembers()
    {
        var group = NewGroup();
        group.AddMember(UserId.New(), Now.AddMinutes(1));
        var before = group.Members.Select(m => m.UserId).ToList();

        group.RemoveMember(UserId.New());

        Assert.Equal(before, group.Members.Select(m => m.UserId));
    }

    [Fact]
    public void RemoveMember_WhenSameMemberTwice_SecondReturnsNotMember()
    {
        var group = NewGroup();
        var member = UserId.New();
        group.AddMember(member, Now.AddMinutes(1));
        group.RemoveMember(member);

        var result = group.RemoveMember(member);

        Assert.Equal(MemberRemoval.NotMember, result);
    }

    [Fact]
    public void RemoveMember_WhenLastMember_ReturnsGroupEmptied()
    {
        var creator = UserId.New();
        var group = Group.Create(ValidName(), creator, InviteCode.Generate(_ => 0),  Now);

        var result = group.RemoveMember(creator);

        Assert.Equal(MemberRemoval.GroupEmptied, result);
    }

    [Fact]
    public void RemoveMember_WhenLastMember_MembersIsEmpty()
    {
        var creator = UserId.New();
        var group = Group.Create(ValidName(), creator, InviteCode.Generate(_ => 0), Now);

        group.RemoveMember(creator);

        Assert.Empty(group.Members);
    }

    [Fact]
    public void AddMember_WhenSeatFreedByRemoval_Succeeds()
    {
        var group = NewGroup();
        var leaving = UserId.New();
        group.AddMember(leaving, Now.AddMinutes(1));
        group.AddMember(UserId.New(), Now.AddMinutes(2));
        group.AddMember(UserId.New(), Now.AddMinutes(3));      // 정원 도달
        group.RemoveMember(leaving);                           // 한 자리 빔
        var newcomer = UserId.New();

        var result = group.AddMember(newcomer, Now.AddMinutes(4));

        Assert.False(result.IsFailure(out var error), error?.Code);
        Assert.Equal(4, group.Members.Count);
        Assert.Contains(group.Members, m => m.UserId == newcomer);
    }


    [Fact]
    public void AddMember_WhenGroupEmptied_Throws()
    {
        var creator = UserId.New();
        var group = Group.Create(ValidName(), creator, InviteCode.Generate(_ => 0), Now);
        group.RemoveMember(creator);

        var newUser = UserId.New();

        Assert.Throws<InvalidOperationException>(() => group.AddMember(newUser, Now));
    }

    [Fact]
    public void Create_WhenNameIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Group.Create(null!, UserId.New(), InviteCode.Generate(_ => 0), Now));
    }

    [Fact]
    public void Create_WhenInviteCodeIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Group.Create(ValidName(), UserId.New(), null!, Now));
    }

    // DateTimeOffset은 attribute 인자(InlineData)가 될 수 없어 TheoryData로 넘긴다.
    public static TheoryData<DateTimeOffset> InvalidTimes =>
    [
        default,                                                          // 호출부가 시각을 빠뜨림
        new DateTimeOffset(2026, 10, 13, 9, 0, 0, TimeSpan.FromHours(9)),   // DateTimeOffset.Now 직접 호출
    ];

    [Theory]
    [MemberData(nameof(InvalidTimes))]
    public void Create_WhenCreatedAtIsInvalid_ThrowsWithParamName(DateTimeOffset createdAt)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), createdAt));

        Assert.Equal("createdAt", ex.ParamName);
    }

    // GroupMember.Create는 internal → 공개 경로 AddMember로 검증한다.
    // Group.Create 경로는 Group의 가드가 먼저 던져 GroupMember의 가드까지 가지 않는다.
    // 중복·정원 실패에 걸리지 않도록 만든 회원과 다른 새 회원으로 합류시킨다.
    [Theory]
    [MemberData(nameof(InvalidTimes))]
    public void AddMember_WhenJoinedAtIsInvalid_ThrowsWithParamName(DateTimeOffset joinedAt)
    {
        var group = NewGroup();

        var ex = Assert.Throws<ArgumentException>(() => group.AddMember(UserId.New(), joinedAt));

        // ParamName은 GroupMember.Create 안의 인자 식 — 테스트 쪽 변수명과 무관하다.
        Assert.Equal("joinedAt", ex.ParamName);
    }

    [Fact]
    public void AddMember_WhenJoinedAtIsBeforeCreatedAt_Throws()
    {
        var group = NewGroup();   // CreatedAt = Now

        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => group.AddMember(UserId.New(), Now.AddTicks(-1)));

        Assert.Equal("joinedAt", ex.ParamName);
        Assert.Single(group.Members);   // 던지기 전에 아무것도 바꾸지 않았다
    }

    [Fact]
    public void AddMember_WhenJoinedAtEqualsCreatedAt_Succeeds()
    {
        var group = NewGroup();

        Assert.True(group.AddMember(UserId.New(), Now).IsSuccess);
    }

    [Fact]
    public void Create_CreatorIsOwnerWithPickedSlot()
    {
        var creator = UserId.New();

        var group = Group.Create(ValidName(), creator, InviteCode.Generate(_ => 0), Now, pickSlot: _ => 2);

        Assert.Equal(creator, group.OwnerId);
        Assert.Equal(2, group.Members.Single().ColorSlot);
    }

    [Fact]
    public void AddMember_PicksOnlyFreeSlots_AllFourDistinct()
    {
        var group = Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), Now, pickSlot: _ => 2);
        for (var i = 0; i < 3; i++)
            Assert.True(group.AddMember(UserId.New(), Now, pickSlot: _ => 0).IsSuccess);

        Assert.Equal([0, 1, 2, 3], group.Members.Select(m => (int)m.ColorSlot).Order());
    }

    [Fact]
    public void AddMember_AfterRemoval_ReusesFreedSlot()
    {
        var group = Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), Now, pickSlot: _ => 0);   // 0
        var leaver = UserId.New();
        group.AddMember(leaver, Now, pickSlot: _ => 0);                                                            // 1
        group.AddMember(UserId.New(), Now, pickSlot: _ => 0);                                                      // 2
        group.RemoveMember(leaver);

        group.AddMember(UserId.New(), Now, pickSlot: _ => 0);

        Assert.Equal([0, 1, 2], group.Members.Select(m => (int)m.ColorSlot).Order());
    }

    [Fact]
    public void RemoveMember_WhenOwnerLeaves_OwnershipGoesToEarliestJoined()
    {
        var owner = UserId.New();
        var (early, late) = (UserId.New(), UserId.New());
        var group = Group.Create(ValidName(), owner, InviteCode.Generate(_ => 0), Now);
        group.AddMember(late, Now.AddMinutes(10));
        group.AddMember(early, Now.AddMinutes(5));

        Assert.Equal(MemberRemoval.Removed, group.RemoveMember(owner));
        Assert.Equal(early, group.OwnerId);
    }

    [Fact]
    public void RemoveMember_WhenOtherLeaves_OwnerUnchanged()
    {
        var (owner, other) = (UserId.New(), UserId.New());
        var group = Group.Create(ValidName(), owner, InviteCode.Generate(_ => 0), Now);
        group.AddMember(other, Now);

        group.RemoveMember(other);

        Assert.Equal(owner, group.OwnerId);
    }

    [Fact]
    public void Rename_ByOwner_ChangesName()
    {
        var owner = UserId.New();
        var group = Group.Create(ValidName(), owner, InviteCode.Generate(_ => 0), Now);

        Assert.True(group.Rename(GroupName.From("새 이름"), owner).IsSuccess);
        Assert.Equal("새 이름", group.Name.Value);
    }

    [Fact]
    public void Rename_ByMember_ReturnsNotOwner()
    {
        var member = UserId.New();
        var group = Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), Now);
        group.AddMember(member, Now);

        Assert.True(group.Rename(GroupName.From("새 이름"), member).IsFailure(out var error));
        Assert.Equal(GroupErrors.NotOwner, error);
        Assert.Equal("주말 산책", group.Name.Value);
    }

    private static Group NewGroup() =>
        Group.Create(ValidName(), UserId.New(), InviteCode.Generate(_ => 0), Now);

    private static GroupName ValidName()
    {
        Assert.True(GroupName.Create("주말 산책").TryGetValue(out var name, out _));
        return name;
    }
}

