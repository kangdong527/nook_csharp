using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Application.Places;
using Nook.Domain.Places;
using Nook.Infrastructure;
using Nook.Infrastructure.Persistence;
using Nook.Integration.Tests.Infrastructure;
using Nook.SharedKernel;

namespace Nook.Integration.Tests.Places;

// PL-01 선택한 장소를 확정한다. C′ 흐름(Find → TryAdd ON CONFLICT DO NOTHING → Find)과 동시 등록(3a)을 실제 PostgreSQL로 본다.
public sealed class ConfirmPlaceTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    //
    // 흐름
    // - Confirm_NewPlace_CreatesAndReturnsIt
    // - Confirm_ExistingPlace_ReturnsStoredValuesWithoutOverwriting   (입력 불신 — 다른 이름을 보내도 저장값)
    // - Confirm_ExistingPlace_SkipsCoordinateValidation               (usecases 1a "새 장소일 때만")
    // - Confirm_NewPlace_WithInvalidCoordinate_ReturnsValidationAndAddsNothing
    // - Confirm_WithInvalidKakaoId_ReturnsValidation                   (null·빈 값·공백)
    //
    // 동시 등록(3a)
    // - TryAdd_WhileOtherInsertUncommitted_WaitsThenReturnsNullOnCommit  (근거 ①: 미커밋 상대를 기다린다)
    // - TryAdd_WhileOtherInsertUncommitted_WaitsThenInsertsOnRollback
    // - Confirm_ConcurrentSamePlace_CreatesOneAndAllGetSameId           (20요청 동시)

    private const string KakaoId = "8130394";

    private readonly ServiceProvider _services;

    public ConfirmPlaceTests(PostgresFixture fixture) : base(fixture)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(fixture.ConnectionString);
        services.AddScoped<ConfirmPlaceHandler>();
        _services = services.BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    // ── 흐름 ──────────────────────────────────────────────

    [Fact]
    public async Task Confirm_NewPlace_CreatesAndReturnsIt()
    {
        var view = (await ConfirmAsync(Command())).Unwrap();

        var stored = await Db.CreateContext().Places.SingleAsync();
        Assert.Equal(stored.Id.Value, view.Id);
        Assert.Equal(KakaoId, view.KakaoPlaceId);
        Assert.Equal("누크 카페", view.Name);
        Assert.Equal(37.5665, view.Latitude);
    }

    [Fact]
    public async Task Confirm_ExistingPlace_ReturnsStoredValuesWithoutOverwriting()
    {
        var first = (await ConfirmAsync(Command())).Unwrap();

        var second = (await ConfirmAsync(Command() with { Name = "바뀐 이름", Latitude = 35.1 })).Unwrap();

        Assert.Equal(first, second);
        var stored = await Db.CreateContext().Places.SingleAsync();
        Assert.Equal("누크 카페", stored.Name);
    }

    [Fact]
    public async Task Confirm_ExistingPlace_SkipsCoordinateValidation()
    {
        var first = (await ConfirmAsync(Command())).Unwrap();

        var second = (await ConfirmAsync(Command() with { Latitude = 999 })).Unwrap();

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Confirm_NewPlace_WithInvalidCoordinate_ReturnsValidationAndAddsNothing()
    {
        var result = await ConfirmAsync(Command() with { Latitude = 90.0001 });

        AssertFailure(result, PlaceErrors.CoordinateLatitudeOutOfRange);
        Assert.Equal(0, await Db.CreateContext().Places.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Confirm_WithInvalidKakaoId_ReturnsValidation(string? kakaoId)
    {
        AssertFailure(await ConfirmAsync(Command() with { KakaoPlaceId = kakaoId }), PlaceErrors.KakaoPlaceIdEmpty);
    }

    // ── 동시 등록 ─────────────────────────────────────────

    // 상대(A)가 같은 카카오 ID를 INSERT하고 커밋 전에 멈춘 상태를 만든다. B는 결과를 기다려야 한다.
    [Fact]
    public async Task TryAdd_WhileOtherInsertUncommitted_WaitsThenReturnsNullOnCommit()
    {
        await using var a = _services.CreateAsyncScope();
        await using var txA = await a.ServiceProvider.GetRequiredService<NookDbContext>().Database.BeginTransactionAsync();
        Assert.NotNull(await Repository(a).TryAddAsync(NewPlace(), CancellationToken.None));

        await using var b = _services.CreateAsyncScope();
        var tryB = Repository(b).TryAddAsync(NewPlace(), CancellationToken.None);

        await LockWait.AssertBlockedByAsync(Db, a.ServiceProvider.GetRequiredService<NookDbContext>(), tryB);
        await txA.CommitAsync();

        Assert.Null(await tryB);
        Assert.Equal(1, await Db.CreateContext().Places.CountAsync());
    }

    [Fact]
    public async Task TryAdd_WhileOtherInsertUncommitted_WaitsThenInsertsOnRollback()
    {
        await using var a = _services.CreateAsyncScope();
        await using var txA = await a.ServiceProvider.GetRequiredService<NookDbContext>().Database.BeginTransactionAsync();
        await Repository(a).TryAddAsync(NewPlace(), CancellationToken.None);

        await using var b = _services.CreateAsyncScope();
        var placeB = NewPlace();
        var tryB = Repository(b).TryAddAsync(placeB, CancellationToken.None);

        await LockWait.AssertBlockedByAsync(Db, a.ServiceProvider.GetRequiredService<NookDbContext>(), tryB);
        await txA.RollbackAsync();

        var added = await tryB;
        Assert.Equal(placeB.Id, added?.Id);
    }

    [Fact]
    public async Task Confirm_ConcurrentSamePlace_CreatesOneAndAllGetSameId()
    {
        const int requests = 20;
        // 모두 출발선에 세운 뒤 한 번에 놓는다 — 먼저 시작한 요청이 혼자 끝나 버리면 경합이 생기지 않는다.
        var start = new TaskCompletionSource();
        var calls = Enumerable.Range(0, requests).Select(async _ =>
        {
            await start.Task;
            return await ConfirmAsync(Command());
        }).ToList();

        start.SetResult();
        var results = await Task.WhenAll(calls);

        var ids = results.Select(r => r.Unwrap().Id).Distinct();
        Assert.Single(ids);
        Assert.Equal(1, await Db.CreateContext().Places.CountAsync());
    }

    // ── 도우미 ────────────────────────────────────────────

    private static ConfirmPlaceCommand Command() => new(KakaoId, "누크 카페", "서울 중구 세종대로 110", 37.5665, 126.978);

    private static Place NewPlace() =>
        Place.Create(KakaoPlaceId.From(KakaoId), "누크 카페", "서울", Coordinate.Create(37.5665, 126.978).Unwrap()).Unwrap();

    private static IPlaceRepository Repository(AsyncServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IPlaceRepository>();

    // 요청마다 새 스코프 = 요청당 DbContext(연결) 하나.
    private async Task<Result<PlaceView>> ConfirmAsync(ConfirmPlaceCommand command)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ConfirmPlaceHandler>().HandleAsync(command, CancellationToken.None);
    }

    private static void AssertFailure<T>(Result<T> result, Error expected)
    {
        Assert.False(result.TryGetValue(out _, out var error));
        Assert.Equal(expected, error);
    }
}
