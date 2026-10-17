using Microsoft.EntityFrameworkCore;
using Nook.Domain.Groups;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence.Configurations;
using Nook.Infrastructure.Persistence.Converters;

namespace Nook.Infrastructure.Persistence;

public sealed class NookDbContext(DbContextOptions<NookDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // 강타입 ID는 타입 단위로 한 번만 등록한다. Configuration마다 HasConversion을 반복하면 빠뜨릴 수 있다.
        configurationBuilder.Properties<UserId>().HaveConversion<EntityIdConverter<UserId>>();
        configurationBuilder.Properties<PlaceId>().HaveConversion<EntityIdConverter<PlaceId>>();
        configurationBuilder.Properties<ReviewId>().HaveConversion<EntityIdConverter<ReviewId>>();
        configurationBuilder.Properties<ReviewImageId>().HaveConversion<EntityIdConverter<ReviewImageId>>();
        configurationBuilder.Properties<GroupId>().HaveConversion<EntityIdConverter<GroupId>>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
