using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 reviews + review_images. ReviewImage는 애그리거트 내부 엔티티라 OwnsMany로 Review와 함께 로딩·저장된다.
// ux_review_images_review_sort_order(DEFERRABLE)는 EF 모델로 표현할 수 없어 마이그레이션 SQL로 만든다(erd.md review_images 확인 항목).
internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", table =>
        {
            table.HasCheckConstraint("ck_reviews_content", $"char_length(btrim(content)) >= {ReviewContent.MinLength}");
            table.HasCheckConstraint("ck_reviews_version", "version >= 0");
        });

        // 받는 쿼리: erd.md reviews Q1
        builder.HasKey(r => r.Id).HasName("pk_reviews");

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.PlaceId).HasColumnName("place_id");

        builder.Property(r => r.Content)
            .HasColumnName("content")
            .HasMaxLength(ReviewContent.MaxLength)
            .HasConversion(c => c.Value, value => ReviewContent.From(value));

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.ReviewDate).HasColumnName("review_date");

        // 동시성 토큰: UPDATE reviews ... WHERE id = ? AND version = <읽은 값>. 0행이면 DbUpdateConcurrencyException(RV-02 6c, incident 002).
        builder.Property(r => r.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        // 받는 쿼리: erd.md reviews Q5 (R-RV-03 중복 방지), 앞 컬럼으로 Q3·Q4·Q6
        builder.HasIndex(r => new { r.UserId, r.PlaceId, r.ReviewDate })
            .IsUnique()
            .HasDatabaseName("ux_reviews_user_place_date");

        // 받는 쿼리: erd.md reviews Q2, places 삭제 RESTRICT 검사
        builder.HasIndex(r => new { r.PlaceId, r.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_reviews_place_created");

        // PF-02: 탈퇴해도 리뷰는 남는다(PM) → SET NULL(PF-02 ⓪ (가)). UNIQUE(user_id, place_id, review_date)는 NULL끼리 다르다고 보므로
        // 탈퇴 회원 여러 명의 같은 날 리뷰가 공존할 수 있다 — NULLS NOT DISTINCT로 바꾸면 안 된다(erd.md reviews 메모).
        builder.HasOne<User>().WithMany()
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("fk_reviews_users")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Place>().WithMany()
            .HasForeignKey(r => r.PlaceId)
            .HasConstraintName("fk_reviews_places")
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(r => r.Images, images =>
        {
            images.ToTable("review_images", table =>
            {
                table.HasCheckConstraint("ck_review_images_sort_order", $"sort_order BETWEEN 0 AND {Review.MaxImages - 1}");
                table.HasCheckConstraint("ck_review_images_storage_key", "btrim(storage_key) <> ''");
            });

            // 부모 ID는 Domain에 없다(내부 엔티티는 부모를 모름, D2) → 섀도 속성
            images.WithOwner().HasForeignKey("ReviewId").HasConstraintName("fk_review_images_reviews");
            images.Property<ReviewId>("ReviewId").HasColumnName("review_id");

            // 받는 쿼리: erd.md review_images Q2
            images.HasKey(i => i.Id).HasName("pk_review_images");
            images.Property(i => i.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            images.Property(i => i.StorageKey)
                .HasColumnName("storage_key")
                .HasMaxLength(ReviewImage.StorageKeyMaxLength);

            // 받는 쿼리: erd.md review_images Q4 (리뷰 간 키 공유 방지)
            images.HasIndex(i => i.StorageKey)
                .IsUnique()
                .HasDatabaseName("ux_review_images_storage_key");

            images.Property(i => i.SortOrder)
                .HasColumnName("sort_order")
                .HasConversion<short>();   // Domain int ↔ smallint
        });

        // Images 게터는 매번 정렬한 복사본을 만든다 → EF는 게터가 아니라 _images 필드를 직접 읽고 쓴다
        builder.Navigation(r => r.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
