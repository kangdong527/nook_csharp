using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Nook.Domain.Bookmarks;
using Nook.Domain.Groups;
using Nook.Domain.Likes;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence.Configurations;
using Nook.Infrastructure.Persistence.Converters;

namespace Nook.Infrastructure.Persistence;

public sealed class NookDbContext(DbContextOptions<NookDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Group> Groups => Set<Group>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // 강타입 ID는 타입 단위로 한 번만 등록한다. Configuration마다 HasConversion을 반복하면 빠뜨릴 수 있다.
        configurationBuilder.Properties<UserId>().HaveConversion<EntityIdConverter<UserId>>();
        configurationBuilder.Properties<PlaceId>().HaveConversion<EntityIdConverter<PlaceId>>();
        configurationBuilder.Properties<ReviewId>().HaveConversion<EntityIdConverter<ReviewId>>();
        configurationBuilder.Properties<ReviewImageId>().HaveConversion<EntityIdConverter<ReviewImageId>>();
        configurationBuilder.Properties<GroupId>().HaveConversion<EntityIdConverter<GroupId>>();

        // EF는 FK 컬럼마다 인덱스를 자동으로 만든다(IX_bookmarks_place_id 등). 인덱스는 쿼리에서 출발해
        // erd.md에 적은 것만 둔다 — 자동 생성을 끄고 Configuration에 명시한 인덱스만 만든다.
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PlaceConfiguration());
        modelBuilder.ApplyConfiguration(new BookmarkConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());
        modelBuilder.ApplyConfiguration(new LikeConfiguration());
        modelBuilder.ApplyConfiguration(new GroupConfiguration());
        // DbSet이 없다: MagicLinkToken은 internal이고, Infrastructure 밖에서 이 테이블을 직접 다루지 않는다.
        modelBuilder.ApplyConfiguration(new MagicLinkTokenConfiguration());
    }
}
