using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence.Auth;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 magic_link_tokens. 이름은 erd.md 그대로 명시한다.
// ix_magic_link_tokens_email_lower_created(lower() 표현식 인덱스)는 HasIndex로 표현할 수 없어 마이그레이션의 SQL로 만든다.
internal sealed class MagicLinkTokenConfiguration : IEntityTypeConfiguration<MagicLinkToken>
{
    public void Configure(EntityTypeBuilder<MagicLinkToken> builder)
    {
        builder.ToTable("magic_link_tokens", table =>
        {
            table.HasCheckConstraint("ck_magic_link_tokens_token_hash", "octet_length(token_hash) = 32");
            table.HasCheckConstraint("ck_magic_link_tokens_expires", "expires_at > created_at");
            table.HasCheckConstraint("ck_magic_link_tokens_consumed",
                "consumed_at IS NULL OR consumed_at >= created_at");
        });

        // 받는 쿼리: erd.md magic_link_tokens Q1
        builder.HasKey(t => t.TokenHash).HasName("pk_magic_link_tokens");

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .ValueGeneratedNever();

        builder.Property(t => t.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.ConsumedAt).HasColumnName("consumed_at");
    }
}
