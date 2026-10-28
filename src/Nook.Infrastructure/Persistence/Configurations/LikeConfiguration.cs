using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Likes;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence.Converters;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 likes. PK는 review_id가 앞(리뷰별 좋아요 수가 자주 실행됨), user_id 단독은 보조 인덱스.
internal sealed class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("likes");

        // PF-02: 대리 키(섀도) — 탈퇴하면 user_id가 NULL이 되어 (review_id, user_id)를 PK로 둘 수 없다. 값은 앱이 v7로(ADR-006).
        builder.Property<Guid>("Id").HasColumnName("id").HasValueGenerator<GuidV7Generator>();
        builder.HasKey("Id").HasName("pk_likes");

        // 받는 쿼리: erd.md likes Q1·Q2·Q3·Q4 (R-LK-01 중복 방지 — PK에서 UNIQUE로, 컬럼 순서는 그대로 review_id 앞).
        // NULL(탈퇴 회원)끼리는 다르다고 본다 → 탈퇴 회원 여러 명의 좋아요가 한 리뷰에 남을 수 있다.
        builder.HasIndex(l => new { l.ReviewId, l.UserId })
            .IsUnique()
            .HasDatabaseName("ux_likes_review_user");

        builder.Property(l => l.ReviewId).HasColumnName("review_id");
        builder.Property(l => l.UserId).HasColumnName("user_id");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");

        // 받는 쿼리: erd.md likes Q5 (탈퇴 CASCADE·FK 검사)
        builder.HasIndex(l => l.UserId).HasDatabaseName("ix_likes_user_id");

        // PF-02: 탈퇴해도 좋아요는 남는다(PM) → SET NULL.
        builder.HasOne<User>().WithMany()
            .HasForeignKey(l => l.UserId)
            .HasConstraintName("fk_likes_users")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Review>().WithMany()
            .HasForeignKey(l => l.ReviewId)
            .HasConstraintName("fk_likes_reviews")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
