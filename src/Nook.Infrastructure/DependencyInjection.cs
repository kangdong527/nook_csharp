using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nook.Application.Auth;
using Nook.Application.Bookmarks;
using Nook.Application.Common;
using Nook.Application.Likes;
using Nook.Application.Places;
using Nook.Application.Reviews;
using Nook.Application.Users;
using Nook.Infrastructure.Auth;
using Nook.Infrastructure.Bookmarks;
using Nook.Infrastructure.Likes;
using Nook.Infrastructure.Persistence;
using Nook.Infrastructure.Places;
using Nook.Infrastructure.Reviews;
using Nook.Infrastructure.Users;

namespace Nook.Infrastructure;

public static class DependencyInjection
{
    // Api는 공급자(Npgsql)를 모르고 연결 문자열만 넘긴다.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<NookDbContext>(options => options.UseNpgsql(connectionString));

        // 저장소·트랜잭션은 DbContext와 같은 수명(요청당 하나) — 같은 DbContext를 받아야 한 트랜잭션에 묶인다
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IMagicLinkTokenStore, MagicLinkTokenStore>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();

        services.AddOptions<MagicLinkOptions>()
            .BindConfiguration(MagicLinkOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 큐는 인스턴스 하나를 두 이름으로 꺼낸다: 핸들러는 IMagicLinkMailQueue(쓰기), 워커는 구현 클래스(Reader).
        // AddSingleton<IMagicLinkMailQueue, ChannelMagicLinkMailQueue>()를 따로 부르면 인스턴스가 둘이 되어 워커가 아무것도 못 받는다.
        services.AddSingleton<ChannelMagicLinkMailQueue>();
        services.AddSingleton<IMagicLinkMailQueue>(sp => sp.GetRequiredService<ChannelMagicLinkMailQueue>());
        // 워커는 IEmailTransport가 필요하다. 발송기를 등록하지 않은 환경(운영, DEP 5일째 전)은 앱 시작에서 실패한다.
        services.AddHostedService<MagicLinkMailWorker>();

        return services;
    }

    // 개발 전용. 메일 본문(로그인 링크)을 로그로 찍는다.
    public static IServiceCollection AddConsoleEmailTransport(this IServiceCollection services) =>
        services.AddSingleton<IEmailTransport, ConsoleEmailTransport>();
}
