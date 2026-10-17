using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Infrastructure.Persistence;

namespace Nook.Infrastructure;

public static class DependencyInjection
{
    // Api는 공급자(Npgsql)를 모르고 연결 문자열만 넘긴다.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<NookDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
