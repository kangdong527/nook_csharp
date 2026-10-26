using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Application.Groups;
using Nook.Domain.Groups;
using Nook.Domain.Users;
using Nook.Infrastructure;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;

namespace Nook.Integration.Tests.Groups;

// GR-02 그룹을 만든다. 그룹 + 첫 멤버의 원자성(R-GR-08)과 초대 코드 충돌 재생성(R-GR-06)을 실제 PostgreSQL로 본다.
public sealed class CreateGroupTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // 흐름
    // - Create_WithValidName_SavesGroupWithCreatorAsOnlyMember
    // - Create_WithInvalidName_ReturnsValidationAndAddsNothing
    // - Create_WhenUserInMaxGroups_ReturnsTooManyGroupsAndAddsNothing   (R-GR-02)
    //
    // 원자성(R-GR-08)
    // - Create_WhenMemberInsertFails_LeavesNoGroup                 (없는 회원 → fk_group_members_users)
    //
    // 초대 코드 충돌(R-GR-06)
    // - Create_WhenInviteCodeCollides_RetriesWithNextCode
    // - Create_WhenEveryAttemptCollides_ThrowsAfterMaxAttempts

    private readonly ServiceProvider _services;
    private readonly QueueInviteCodeGenerator _codes = new();

    public CreateGroupTests(PostgresFixture fixture) : base(fixture)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        // 운영 등록(RandomInviteCodeGenerator) 뒤에 등록 → 마지막 등록이 이긴다.
        services.AddSingleton<IInviteCodeGenerator>(_codes);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateGroupHandler>();
        _services = services.BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    // ── 흐름 ──────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidName_SavesGroupWithCreatorAsOnlyMember()
    {
        var creator = await SeedUserAsync(1);
        _codes.Enqueue("ABCDEF");

        var view = (await CreateAsync(creator.Id, "산책모임")).Unwrap();

        Assert.Equal("산책모임", view.Name);
        Assert.Equal("ABCDEF", view.InviteCode);
        await using var db = Db.CreateContext();
        var stored = await db.Groups.AsNoTracking().SingleAsync();
        Assert.Equal(view.Id, stored.Id.Value);
        Assert.Equal(creator.Id, Assert.Single(stored.Members).UserId);
    }

    [Fact]
    public async Task Create_WithInvalidName_ReturnsValidationAndAddsNothing()
    {
        var creator = await SeedUserAsync(1);
        _codes.Enqueue("ABCDEF");

        var result = await CreateAsync(creator.Id, "가");

        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(GroupErrors.GroupNameLengthOutOfRange, error);
        Assert.Equal(0, await Db.CreateContext().Groups.CountAsync());
    }

    [Fact]
    public async Task Create_WhenUserInMaxGroups_ReturnsTooManyGroupsAndAddsNothing()
    {
        var creator = await SeedUserAsync(1);
        var existing = new[] { "BBBBBB", "CCCCCC", "DDDDDD" }.Take(Group.MaxGroupsPerUser).ToArray();
        foreach (var code in existing)
            await SeedGroupAsync(code, creator.Id);
        _codes.Enqueue("ABCDEF");

        var result = await CreateAsync(creator.Id, "넷째모임");

        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(GroupErrors.TooManyGroups, error);
        Assert.Equal(existing.Length, await Db.CreateContext().Groups.CountAsync());
        Assert.Equal(1, _codes.Remaining);   // 코드를 꺼내기 전에 거절했다
    }

    // ── 원자성 ────────────────────────────────────────────

    // groups INSERT는 성공하고 group_members INSERT가 FK로 실패하는 상황. 한 트랜잭션이면 그룹도 남지 않는다.
    [Fact]
    public async Task Create_WhenMemberInsertFails_LeavesNoGroup()
    {
        var missingUser = UserId.New();
        _codes.Enqueue("ABCDEF");

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => CreateAsync(missingUser, "산책모임"));

        Assert.Contains("fk_group_members_users", ex.InnerException?.Message);
        Assert.Equal(0, await Db.CreateContext().Groups.CountAsync());
    }

    // ── 초대 코드 충돌 ────────────────────────────────────

    [Fact]
    public async Task Create_WhenInviteCodeCollides_RetriesWithNextCode()
    {
        var creator = await SeedUserAsync(1);
        await SeedGroupAsync("ABCDEF");
        _codes.Enqueue("ABCDEF", "BCDEFG");

        var view = (await CreateAsync(creator.Id, "새모임")).Unwrap();

        Assert.Equal("BCDEFG", view.InviteCode);
        Assert.Equal(2, await Db.CreateContext().Groups.CountAsync());
    }

    [Fact]
    public async Task Create_WhenEveryAttemptCollides_ThrowsAfterMaxAttempts()
    {
        var creator = await SeedUserAsync(1);
        await SeedGroupAsync("ABCDEF");
        _codes.Enqueue(Enumerable.Repeat("ABCDEF", CreateGroupHandler.MaxInviteCodeAttempts + 1).ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync(creator.Id, "새모임"));

        Assert.Equal(1, _codes.Remaining);   // 정확히 MaxInviteCodeAttempts번만 꺼냈다
        Assert.Equal(1, await Db.CreateContext().Groups.CountAsync());
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

    // owner를 주지 않으면 100번 회원을 새로 만들어 주인으로 쓴다.
    private async Task SeedGroupAsync(string code, UserId? owner = null)
    {
        owner ??= (await SeedUserAsync(100)).Id;
        var group = Group.Create(GroupName.From("기존모임"), owner, InviteCode.From(code),
            new DateTimeOffset(2026, 10, 18, 9, 0, 0, TimeSpan.Zero));
        await using var db = Db.CreateContext();
        db.Groups.Add(group);
        await db.SaveChangesAsync();
    }

    private async Task<Result<GroupView>> CreateAsync(UserId userId, string name)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CreateGroupHandler>()
            .HandleAsync(new CreateGroupCommand(userId.Value, name), CancellationToken.None);
    }

    // 정해 둔 순서로 코드를 내준다. 다 쓰면 예외 — 핸들러가 예상보다 많이 꺼내면 테스트가 드러낸다.
    private sealed class QueueInviteCodeGenerator : IInviteCodeGenerator
    {
        private readonly Queue<string> _queue = new();

        public int Remaining => _queue.Count;

        public void Enqueue(params string[] codes)
        {
            foreach (var code in codes)
                _queue.Enqueue(code);
        }

        public InviteCode Next() => InviteCode.From(_queue.Dequeue());
    }
}
