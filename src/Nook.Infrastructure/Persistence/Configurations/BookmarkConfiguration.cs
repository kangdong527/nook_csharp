using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Bookmarks;
using Nook.Domain.Places;
using Nook.Domain.Users;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 bookmarks. 보조 인덱스 없음(place_id 단독 조회 유스케이스가 없다).
internal sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("bookmarks");

        // 받는 쿼리: erd.md bookmarks Q1·Q2·Q3 (R-BM-01 중복 방지)
        builder.HasKey(b => new { b.UserId, b.PlaceId }).HasName("pk_bookmarks");

        builder.Property(b => b.UserId).HasColumnName("user_id");
        builder.Property(b => b.PlaceId).HasColumnName("place_id");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");

        builder.HasOne<User>().WithMany()
            .HasForeignKey(b => b.UserId)
            .HasConstraintName("fk_bookmarks_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Place>().WithMany()
            .HasForeignKey(b => b.PlaceId)
            .HasConstraintName("fk_bookmarks_places")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
