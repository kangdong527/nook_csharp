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

// GR-04 그룹에서 나간다. 마지막 멤버 탈퇴 = 그룹 삭제(R-GR-09)와 동시 탈퇴 Write Skew를 실제 PostgreSQL로 본다(D6).
public sealed class LeaveGroupTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // 흐름
    // - Leave_AsMember_RemovesOnlyThatMember
    // - Leave_AsLastMember_DeletesGroup                                  (R-GR-09)
    // - Leave_WhenNotMember_SucceedsAndChangesNothing                    (멱등)
    // - Leave_WhenGroupMissing_Succeeds                                  (멱등 — 마지막 멤버의 두 번째 요청)
    //
    // 동시성
    // - Leave_ConcurrentByLastTwoMembers_DeletesGroup                    (Write Skew, 여러 라운드)
    // - LeaveAndJoin_ConcurrentOnLastMember_NeverLeavesEmptyGroup        (탈퇴와 합류가 겹침)

    private const int Rounds = 10;

    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _services;
    private readonly ITestOutputHelper _output;

    public LeaveGroupTests(PostgresFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<LeaveGroupHandler>();
        services.AddScoped<JoinGroupHandler>();
        _services = services.BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    // ── 흐름 ──────────────────────────────────────────────

    [Fact]
    public async Task Leave_AsMember_RemovesOnlyThatMember()
    {
        var (a, b) = (await SeedUserAsync(1), await SeedUserAsync(2));
        var groupId = await SeedGroupAsync("ABCDEF", a.Id, b.Id);

        var result = await LeaveAsync(b.Id, groupId);

        Assert.True(result.IsSuccess);
        Assert.Equal([a.Id], await MemberIdsAsync(groupId));
    }

    [Fact]
    public async Task Leave_AsLastMember_DeletesGroup()
    {
        var a = await SeedUserAsync(1);
        var groupId = await SeedGroupAsync("ABCDEF", a.Id);

        var result = await LeaveAsync(a.Id, groupId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await GroupCountAsync());
    }

    [Fact]
    public async Task Leave_WhenNotMember_SucceedsAndChangesNothing()
    {
        var (a, outsider) = (await SeedUserAsync(1), await SeedUserAsync(2));
        var groupId = await SeedGroupAsync("ABCDEF", a.Id);

        var result = await LeaveAsync(outsider.Id, groupId);

        Assert.True(result.IsSuccess);
        Assert.Equal([a.Id], await MemberIdsAsync(groupId));
    }

    [Fact]
    public async Task Leave_WhenGroupMissing_Succeeds()
    {
        var a = await SeedUserAsync(1);
        var groupId = await SeedGroupAsync("ABCDEF", a.Id);
        Assert.True((await LeaveAsync(a.Id, groupId)).IsSuccess);

        var second = await LeaveAsync(a.Id, groupId);

        Assert.True(second.IsSuccess);
    }

    // ── 동시성 ────────────────────────────────────────────

    // 남은 두 멤버가 동시에 나간다. 잠금이 없으면 둘 다 "1명 남음"(Removed)으로 판단해 멤버 0명 그룹이 남는다.
    // 경합은 확률적이라 한 번으로는 놓칠 수 있다 → 라운드마다 새 그룹으로 여러 번.
    [Fact]
    public async Task Leave_ConcurrentByLastTwoMembers_DeletesGroup()
    {
        var (a, b) = (await SeedUserAsync(1), await SeedUserAsync(2));

        var emptyGroupsLeft = 0;
        for (var round = 0; round < Rounds; round++)
        {
            var groupId = await SeedGroupAsync(RoundCode(round), a.Id, b.Id);

            var results = await RunTogetherAsync(() => LeaveAsync(a.Id, groupId), () => LeaveAsync(b.Id, groupId));

            Assert.All(results, r => Assert.True(r.IsSuccess));
            if (await GroupExistsAsync(groupId))
                emptyGroupsLeft++;
        }

        _output.WriteLine($"{Rounds}라운드 중 그룹이 남은 라운드: {emptyGroupsLeft}");
        Assert.Equal(0, emptyGroupsLeft);
    }

    // 마지막 멤버 탈퇴와 새 회원의 합류가 겹친다. 어느 쪽이 그룹 행을 먼저 잡든 멤버 0명 그룹은 없어야 한다.
    // 탈퇴가 먼저: 그룹이 지워지고 합류는 NotFound. 합류가 먼저: 2명이 됐다가 1명(합류한 회원)이 남는다.
    [Fact]
    public async Task LeaveAndJoin_ConcurrentOnLastMember_NeverLeavesEmptyGroup()
    {
        var (last, joiner) = (await SeedUserAsync(1), await SeedUserAsync(2));

        var outcomes = new Dictionary<string, int>();
        for (var round = 0; round < Rounds; round++)
        {
            var code = RoundCode(round);
            var groupId = await SeedGroupAsync(code, last.Id);

            var results = await RunTogetherAsync(() => LeaveAsync(last.Id, groupId), () => JoinAsync(joiner.Id, code));

            Assert.True(results[0].IsSuccess);
            if (await GroupExistsAsync(groupId))
            {
                Assert.True(results[1].IsSuccess);
                Assert.Equal([joiner.Id], await MemberIdsAsync(groupId));
                Assert.True((await LeaveAsync(joiner.Id, groupId)).IsSuccess);   // 다음 라운드를 위해 정리(그룹도 지워진다)
                Count("합류 먼저");
            }
            else
            {
                Assert.True(results[1].IsFailure(out var error));
                Assert.Equal(JoinGroupErrors.GroupNotFound, error);
                Count("탈퇴 먼저");
            }
        }

        _output.WriteLine(string.Join(" · ", outcomes.Select(o => $"{o.Key} {o.Value}")));
        return;

        void Count(string key) => outcomes[key] = outcomes.GetValueOrDefault(key) + 1;
    }

    // ── 도우미 ────────────────────────────────────────────

    // 라운드마다 다른 유효 코드: "LEAVE" 뒤에 알파벳 한 글자(O·I는 코드에 못 쓴다).
    private static string RoundCode(int round) => "LEAVE" + InviteCode.Alphabet[round];

    // 출발선에 세운 뒤 한 번에 놓는다(JoinGroupTests와 같은 기법).
    private static async Task<Result[]> RunTogetherAsync(params Func<Task<Result>>[] calls)
    {
        var start = new TaskCompletionSource();
        var tasks = calls.Select(async call =>
        {
            await start.Task;
            return await call();
        }).ToList();

        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    private async Task<User> SeedUserAsync(int n)
    {
        var user = User.Create(Nickname.From($"회원{n}"), Email.From($"user{n}@example.com"), Gender.None, null);
        await using var db = Db.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // 첫 회원이 만든 회원, 나머지는 합류한 멤버.
    private async Task<GroupId> SeedGroupAsync(string code, UserId creator, params UserId[] others)
    {
        var group = Group.Create(GroupName.From("모임"), creator, InviteCode.From(code), CreatedAt);
        foreach (var other in others)
            Assert.True(group.AddMember(other, CreatedAt).IsSuccess);

        await using var db = Db.CreateContext();
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group.Id;
    }

    private async Task<List<UserId>> MemberIdsAsync(GroupId groupId)
    {
        await using var db = Db.CreateContext();
        var group = await db.Groups.AsNoTracking().SingleAsync(g => g.Id == groupId);
        return group.Members.Select(m => m.UserId).ToList();
    }

    private async Task<bool> GroupExistsAsync(GroupId groupId)
    {
        await using var db = Db.CreateContext();
        return await db.Groups.AnyAsync(g => g.Id == groupId);
    }

    private async Task<int> GroupCountAsync()
    {
        await using var db = Db.CreateContext();
        return await db.Groups.CountAsync();
    }

    private async Task<Result> LeaveAsync(UserId userId, GroupId groupId)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LeaveGroupHandler>()
            .HandleAsync(new LeaveGroupCommand(userId.Value, groupId.Value), CancellationToken.None);
    }

    private async Task<Result> JoinAsync(UserId userId, string code)
    {
        await using var scope = _services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<JoinGroupHandler>()
            .HandleAsync(new JoinGroupCommand(userId.Value, code), CancellationToken.None);
        // 응답 모양(GroupView)은 GroupsApiTests가 본다. 여기서는 성공/실패와 에러만.
        return result.TryGetValue(out _, out var error) ? Result.Success() : error;
    }
}
