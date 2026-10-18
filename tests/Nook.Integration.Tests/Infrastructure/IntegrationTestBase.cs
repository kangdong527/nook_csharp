namespace Nook.Integration.Tests.Infrastructure;

// 상속한 테스트 클래스는 같은 컨테이너를 공유하고, 각 테스트 시작 전에 빈 DB를 받는다.
// 같은 컬렉션 안의 테스트는 xUnit이 순차 실행하므로 ResetAsync가 다른 테스트 데이터를 지우지 않는다.
[Collection(PostgresCollection.Name)]
public abstract class IntegrationTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    protected PostgresFixture Db { get; } = fixture;

    public Task InitializeAsync() => Db.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
