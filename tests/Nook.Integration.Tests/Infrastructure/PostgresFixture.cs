using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nook.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Nook.Integration.Tests.Infrastructure;

// 컨테이너는 컬렉션당 한 번 띄운다(기동 수 초). 테스트 간 격리는 ResetAsync로 데이터만 비운다.
// 연결 문자열은 컨테이너가 만들어 준다 — compose·user-secrets에 의존하지 않는다.
public sealed class PostgresFixture : IAsyncLifetime
{
    // compose.yaml과 같은 메이저 버전. 다르면 로컬에서 통과하고 운영에서 깨지는 일이 생긴다.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    private DbContextOptions<NookDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _options = new DbContextOptionsBuilder<NookDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        // EnsureCreated 금지(ADR-008): 마이그레이션에만 있는 raw SQL 제약이 빠진다.
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    // 호출마다 새 DbContext — 저장용과 검증용을 분리해야 식별 맵 캐시가 아닌 DB 값을 읽는다.
    // sqlLog를 넘기면 실행된 SQL(파라미터 값 포함)만 그쪽으로 보낸다. 테스트에서는 ITestOutputHelper.WriteLine.
    public NookDbContext CreateContext(Action<string>? sqlLog = null)
    {
        if (sqlLog is null)
        {
            return new(_options);
        }

        var options = new DbContextOptionsBuilder<NookDbContext>(_options)
            .LogTo(sqlLog, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;
        return new(options);
    }

    // 마이그레이션 이력 테이블만 남기고 public 스키마의 모든 테이블을 비운다.
    // 테이블 목록을 하드코딩하지 않으므로 마이그레이션이 늘어도 고칠 필요가 없다.
    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE tables text;
            BEGIN
                SELECT string_agg(format('%I', tablename), ', ') INTO tables
                FROM pg_tables
                WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory';

                IF tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE ' || tables || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
