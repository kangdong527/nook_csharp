using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Application.Groups;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.Infrastructure;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;
using Xunit.Abstractions;

namespace Nook.Integration.Tests.Groups;

// GR-01 초대 코드로 그룹에 합류한다. 동시 합류(5b, R-GR-03)를 실제 PostgreSQL로 재현한다(D6).
public sealed class JoinGroupTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // 흐름
    // - Join_WithValidCode_AddsMember
    // - Join_WithUnknownCode_ReturnsNotFound
    // - Join_WhenFull_ReturnsGroupFull
    // - Join_WhenUserInMaxGroups_ReturnsTooManyGroups              (3b, R-GR-02)
    // - Join_WhenUserInMaxGroupsIncludingThisOne_ReturnsAlreadyMember (중복이 3개 제한보다 먼저)
    //
    // 동시 합류(5b)
    // - Join_ConcurrentIntoGroupWithThreeSeats_KeepsFourMembers   (빈자리 3, 5명 동시)

    private const string Code = "ABCDEF";

    // timestamptz는 마이크로초까지만 저장한다(GroupPersistenceTests와 같은 이유).
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _services;
    private readonly ITestOutputHelper _output;

    public JoinGroupTests(PostgresFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<JoinGroupHandler>();
        _services = services.BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    // ── 흐름 ──────────────────────────────────────────────

    [Fact]
    public async Task Join_WithValidCode_AddsMember()
    {
        var group = await SeedGroupAsync(extraMembers: 0);
        var joiner = await SeedUserAsync(1);

        var result = await JoinAsync(joiner.Id);

        Assert.True(result.IsSuccess);
        Assert.Contains(joiner.Id, await MemberIdsAsync(group.Id));
    }

    [Fact]
    public async Task Join_WithUnknownCode_ReturnsNotFound()
    {
        var joiner = await SeedUserAsync(1);

        var result = await JoinAsync(joiner.Id, code: "ZZZZZZ");

        AssertFailure(result, JoinGroupErrors.GroupNotFound);
    }

    [Fact]
    public async Task Join_WhenFull_ReturnsGroupFull()
    {
        var group = await SeedGroupAsync(extraMembers: Group.MaxGroupMembers - 1);
        var joiner = await SeedUserAsync(99);

        var result = await JoinAsync(joiner.Id);

        AssertFailure(result, GroupErrors.GroupFull);
        Assert.Equal(Group.MaxGroupMembers, (await MemberIdsAsync(group.Id)).Count);
    }

    [Fact]
    public async Task Join_WhenUserInMaxGroups_ReturnsTooManyGroups()
    {
        var group = await SeedGroupAsync(extraMembers: 0);
        var joiner = await SeedUserAsync(1);
        foreach (var code in new[] { "BBBBBB", "CCCCCC", "DDDDDD" }.Take(Group.MaxGroupsPerUser))
            await SeedGroupCreatedByAsync(joiner.Id, code);

        var result = await JoinAsync(joiner.Id);

        AssertFailure(result, GroupErrors.TooManyGroups);
        Assert.DoesNotContain(joiner.Id, await MemberIdsAsync(group.Id));
    }

    // 3개 중 하나에 다시 합류 — TooManyGroups가 아니라 AlreadyMember(D6 사용자 결정).
    [Fact]
    public async Task Join_WhenUserInMaxGroupsIncludingThisOne_ReturnsAlreadyMember()
    {
        var joiner = await SeedUserAsync(1);
        foreach (var code in new[] { Code, "CCCCCC", "DDDDDD" }.Take(Group.MaxGroupsPerUser))
            await SeedGroupCreatedByAsync(joiner.Id, code);

        var result = await JoinAsync(joiner.Id);

        AssertFailure(result, GroupErrors.AlreadyMember);
    }

    // ── 동시 합류 ─────────────────────────────────────────

    // 만든 회원 1명 + 빈자리 3. 5명이 동시에 합류하면 3명 성공, 2명 GroupFull, 최종 4명이어야 한다(R-GR-01·03).
    [Fact]
    public async Task Join_ConcurrentIntoGroupWithThreeSeats_KeepsFourMembers()
    {
        const int requests = 5;
        var group = await SeedGroupAsync(extraMembers: 0);
        var joiners = new List<User>();
        for (var i = 1; i <= requests; i++)
            joiners.Add(await SeedUserAsync(i));

        // 모두 출발선에 세운 뒤 한 번에 놓는다(ConfirmPlaceTests와 같은 기법).
        var start = new TaskCompletionSource();
        var calls = joiners.Select(async u =>
        {
            await start.Task;
            return await JoinAsync(u.Id);
        }).ToList();

        start.SetResult();
        var results = await Task.WhenAll(calls);

        var members = await MemberIdsAsync(group.Id);
        _output.WriteLine($"성공 {results.Count(r => r.IsSuccess)} · 실패 {results.Count(r => !r.IsSuccess)} · DB 멤버 {members.Count}명");

        Assert.Equal(Group.MaxGroupMembers, members.Count);
        Assert.Equal(Group.MaxGroupMembers - 1, results.Count(r => r.IsSuccess));
        Assert.All(results.Where(r => !r.IsSuccess), r => AssertFailure(r, GroupErrors.GroupFull));
    }

    // ── 도우미 ────────────────────────────────────────────

    private async Task<User> SeedUserAsync(int n)
    {
        var user = User.Create(Nickname.From($"회원{n}"), Email.From($"user{n}@example.com"), Gender.None, null);
        await using var db = Db.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // 만든 회원 + extraMembers명. 합류 테스트의 회원 번호(1~)와 겹치지 않게 100번대를 쓴다.
    private async Task<Group> SeedGroupAsync(int extraMembers)
    {
        var creator = await SeedUserAsync(100);
        var group = Group.Create(GroupName.From("산책모임"), creator.Id, InviteCode.From(Code), CreatedAt);
        for (var i = 1; i <= extraMembers; i++)
        {
            var member = await SeedUserAsync(100 + i);
            Assert.True(group.AddMember(member.Id, CreatedAt).IsSuccess);
        }

        await using var db = Db.CreateContext();
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private async Task SeedGroupCreatedByAsync(UserId creator, string code)
    {
        await using var db = Db.CreateContext();
        db.Groups.Add(Group.Create(GroupName.From("다른모임"), creator, InviteCode.From(code), CreatedAt));
        await db.SaveChangesAsync();
    }

    private async Task<List<UserId>> MemberIdsAsync(GroupId groupId)
    {
        await using var db = Db.CreateContext();
        var group = await db.Groups.AsNoTracking().SingleAsync(g => g.Id == groupId);
        return group.Members.Select(m => m.UserId).ToList();
    }

    // 요청마다 새 스코프 = 요청당 DbContext(연결) 하나.
    private async Task<Result> JoinAsync(UserId userId, string code = Code)
    {
        await using var scope = _services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<JoinGroupHandler>()
            .HandleAsync(new JoinGroupCommand(userId.Value, code), CancellationToken.None);
        // 응답 모양(GroupView)은 GroupsApiTests가 본다. 여기서는 성공/실패와 에러만.
        return result.TryGetValue(out _, out var error) ? Result.Success() : error;
    }

    private static void AssertFailure(Result result, Error expected)
    {
        Assert.True(result.IsFailure(out var error));
        Assert.Equal(expected, error);
    }
}
