using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Application.Common;
using Nook.Application.Groups;
using Nook.Application.Users;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.Infrastructure;
using Nook.Infrastructure.Persistence;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;
using Npgsql;
using Xunit.Abstractions;

namespace Nook.Integration.Tests.Groups;

// R-GR-02(회원당 3개)·R-GR-04(users 행 잠금)를 실제 PostgreSQL로 본다(D6).
// 그룹 행 잠금(ADR-010)은 서로 다른 그룹에 들어가는 같은 회원의 요청을 막지 못한다 — 공통 행은 users 행뿐이다.
public sealed class GroupMembershipLimitTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // 경합(5a)
    // - JoinAndCreate_ConcurrentForUserInTwoGroups_KeepsThreeGroups     (참여 2개 회원이 합류 2 + 생성 1 동시)
    //
    // 잠금 강도(FOR NO KEY UPDATE, D6 사용자 결정)
    // - UserLock_BlocksSecondLockOnSameUser                             (같은 잠금끼리는 줄 선다)
    // - UserLock_DoesNotBlockForeignKeyInsertForSameUser                (FK 검사 FOR KEY SHARE는 지나간다)
    //
    // 잠금 순서(users → groups, D6 사용자 결정)
    // - ReversedLockOrder_Deadlocks_AndPostgresAbortsOne                (순서를 어기면 무슨 일이 나는가)

    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _services;
    private readonly ITestOutputHelper _output;

    public GroupMembershipLimitTests(PostgresFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<JoinGroupHandler>();
        services.AddScoped<CreateGroupHandler>();
        _services = services.BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    // ── 경합 ──────────────────────────────────────────────

    // 빈자리는 1개(3 - 2). 세 요청이 각자 "2개 < 3"을 읽고 들어가면 5개가 된다. 성공 1, TooManyGroups 2, 최종 3개여야 한다.
    [Fact]
    public async Task JoinAndCreate_ConcurrentForUserInTwoGroups_KeepsThreeGroups()
    {
        var user = await SeedUserAsync(1);
        await SeedGroupAsync(user.Id, "BBBBBB");
        await SeedGroupAsync(user.Id, "CCCCCC");
        await SeedGroupAsync((await SeedUserAsync(101)).Id, "XXXXXX");
        await SeedGroupAsync((await SeedUserAsync(102)).Id, "YYYYYY");

        var start = new TaskCompletionSource();
        var calls = new List<Task<Result>>
        {
            Run(() => JoinAsync(user.Id, "XXXXXX")),
            Run(() => JoinAsync(user.Id, "YYYYYY")),
            Run(() => CreateAsync(user.Id)),
        };

        start.SetResult();
        var results = await Task.WhenAll(calls);

        var memberships = await MembershipCountAsync(user.Id);
        _output.WriteLine($"성공 {results.Count(r => r.IsSuccess)} · 실패 {results.Count(r => !r.IsSuccess)} · 참여 그룹 {memberships}개");

        Assert.Equal(Group.MaxGroupsPerUser, memberships);
        Assert.Single(results, r => r.IsSuccess);
        Assert.All(results.Where(r => !r.IsSuccess), r =>
        {
            Assert.True(r.IsFailure(out var error));
            Assert.Equal(GroupErrors.TooManyGroups, error);
        });
        return;

        // 출발선에 세운 뒤 한 번에 놓는다(JoinGroupTests와 같은 기법).
        async Task<Result> Run(Func<Task<Result>> call)
        {
            await start.Task;
            return await call();
        }
    }

    // ── 잠금 강도 ─────────────────────────────────────────

    [Fact]
    public async Task UserLock_BlocksSecondLockOnSameUser()
    {
        var user = await SeedUserAsync(1);
        await using var holder = _services.CreateAsyncScope();
        await using var tx = await holder.ServiceProvider.GetRequiredService<IUnitOfWork>().BeginAsync(CancellationToken.None);
        await holder.ServiceProvider.GetRequiredService<IUserRepository>().LockAsync(user.Id, CancellationToken.None);

        await using var other = _services.CreateAsyncScope();
        await using var otherTx = await other.ServiceProvider.GetRequiredService<IUnitOfWork>().BeginAsync(CancellationToken.None);
        var second = other.ServiceProvider.GetRequiredService<IUserRepository>().LockAsync(user.Id, CancellationToken.None);

        await LockWait.AssertBlockedByAsync(Db, holder.ServiceProvider.GetRequiredService<NookDbContext>(), second);

        await tx.DisposeAsync();   // 롤백 = 잠금 해제 → 두 번째가 지나간다
        await second;
    }

    // 잠금을 쥔 동안 같은 회원을 참조하는 INSERT(group_members.user_id FK 검사 = users 행 FOR KEY SHARE)가 기다리지 않아야 한다.
    // FOR UPDATE로 바꾸면 이 INSERT가 잠금 해제까지 멈춰 WaitAsync가 시간 초과로 실패한다.
    [Fact]
    public async Task UserLock_DoesNotBlockForeignKeyInsertForSameUser()
    {
        var user = await SeedUserAsync(1);
        await using var holder = _services.CreateAsyncScope();
        await using var tx = await holder.ServiceProvider.GetRequiredService<IUnitOfWork>().BeginAsync(CancellationToken.None);
        await holder.ServiceProvider.GetRequiredService<IUserRepository>().LockAsync(user.Id, CancellationToken.None);

        await SeedGroupAsync(user.Id, "BBBBBB").WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, await MembershipCountAsync(user.Id));
    }

    // ── 잠금 순서 ─────────────────────────────────────────

    // 앱 코드가 아니라 PostgreSQL의 동작을 확인하는 학습 테스트. 두 트랜잭션이 같은 두 행을 반대 순서로 잠근다.
    // PostgreSQL은 deadlock_timeout(기본 1초) 뒤 대기 고리를 찾아 한쪽을 40P01로 롤백하고, 다른 쪽은 진행한다.
    [Fact]
    public async Task ReversedLockOrder_Deadlocks_AndPostgresAbortsOne()
    {
        var user = await SeedUserAsync(1);
        var groupId = await SeedGroupAsync((await SeedUserAsync(101)).Id, "XXXXXX");

        await using var t1 = Db.CreateContext();
        await using var t2 = Db.CreateContext();
        await using var tx1 = await t1.Database.BeginTransactionAsync();
        await using var tx2 = await t2.Database.BeginTransactionAsync();

        await LockUserAsync(t1);                     // T1: users → groups (정한 순서)
        await LockGroupAsync(t2);                    // T2: groups → users (어긴 순서)

        var t1Second = LockGroupAsync(t1);           // T2가 쥔 그룹 행을 기다린다
        await LockWait.AssertBlockedByAsync(Db, t2, t1Second);
        var t2Second = LockUserAsync(t2);            // T1이 쥔 회원 행을 기다린다 → 고리

        var outcomes = await Task.WhenAll(Capture(t1Second), Capture(t2Second));
        _output.WriteLine($"T1: {outcomes[0]?.SqlState ?? "성공"} · T2: {outcomes[1]?.SqlState ?? "성공"}");

        Assert.Single(outcomes, e => e is null);
        Assert.Single(outcomes, e => e?.SqlState == PostgresErrorCodes.DeadlockDetected);
        return;

        Task LockUserAsync(DbContext db) =>
            db.Database.ExecuteSqlAsync($"SELECT 1 FROM users WHERE id = {user.Id.Value} FOR NO KEY UPDATE");

        Task LockGroupAsync(DbContext db) =>
            db.Database.ExecuteSqlAsync($"SELECT 1 FROM groups WHERE id = {groupId.Value} FOR UPDATE");

        static async Task<PostgresException?> Capture(Task task)
        {
            try
            {
                await task;
                return null;
            }
            catch (PostgresException ex)
            {
                return ex;
            }
        }
    }

    // ── 도우미 ────────────────────────────────────────────

    private async Task<User> SeedUserAsync(int n)
    {
        var user = User.Create(Nickname.From($"회원{n}"), Email.From($"user{n}@example.com"));
        await using var db = Db.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<GroupId> SeedGroupAsync(UserId creator, string code)
    {
        var group = Group.Create(GroupName.From("모임"), creator, InviteCode.From(code), CreatedAt);
        await using var db = Db.CreateContext();
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group.Id;
    }

    private async Task<int> MembershipCountAsync(UserId userId)
    {
        await using var db = Db.CreateContext();
        return await db.Groups.CountAsync(g => g.Members.Any(m => m.UserId == userId));
    }

    private async Task<Result> JoinAsync(UserId userId, string code)
    {
        await using var scope = _services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<JoinGroupHandler>()
            .HandleAsync(new JoinGroupCommand(userId.Value, code), CancellationToken.None);
        // 응답 모양(GroupView)은 GroupsApiTests가 본다. 여기서는 성공/실패와 에러만.
        return result.TryGetValue(out _, out var error) ? Result.Success() : error;
    }

    private async Task<Result> CreateAsync(UserId userId)
    {
        await using var scope = _services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<CreateGroupHandler>()
            .HandleAsync(new CreateGroupCommand(userId.Value, "새모임"), CancellationToken.None);
        return result.TryGetValue(out _, out var error) ? Result.Success() : error;
    }
}
