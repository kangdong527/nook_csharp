using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Likes;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 likes. PK는 review_id가 앞(리뷰별 좋아요 수가 자주 실행됨), user_id 단독은 보조 인덱스.
internal sealed class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("likes");

        // 받는 쿼리: erd.md likes Q1·Q2·Q3·Q4 (R-LK-01 중복 방지)
        builder.HasKey(l => new { l.ReviewId, l.UserId }).HasName("pk_likes");

        builder.Property(l => l.ReviewId).HasColumnName("review_id");
        builder.Property(l => l.UserId).HasColumnName("user_id");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");

        // 받는 쿼리: erd.md likes Q5 (탈퇴 CASCADE·FK 검사)
        builder.HasIndex(l => l.UserId).HasDatabaseName("ix_likes_user_id");

        builder.HasOne<User>().WithMany()
            .HasForeignKey(l => l.UserId)
            .HasConstraintName("fk_likes_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Review>().WithMany()
            .HasForeignKey(l => l.ReviewId)
            .HasConstraintName("fk_likes_reviews")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
