using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Users;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 users. 이름은 erd.md 그대로 명시한다.
// ux_users_nickname_lower·ux_users_email_lower(lower() 표현식 UNIQUE)는 HasIndex로 표현할 수 없어 마이그레이션의 SQL로 만든다.
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_nickname",
                "char_length(nickname) >= 2 AND nickname ~ '^[가-힣A-Za-z0-9]+$' AND nickname IS NFC NORMALIZED");
            table.HasCheckConstraint("ck_users_email",
                "email ~ '^[^@[:space:]]+@[^@[:space:]]+$'");
            table.HasCheckConstraint("ck_users_gender",
                "gender IN ('female', 'male', 'none')");
            // 상한(오늘)은 넣지 않는다 — current_date는 비결정적이라 CHECK에 쓰지 않는다(Birthdate 주석).
            table.HasCheckConstraint("ck_users_birthdate",
                "birthdate >= DATE '1900-01-01'");
        });

        // 받는 쿼리: erd.md users Q1·Q2·Q5
        builder.HasKey(u => u.Id).HasName("pk_users");

        // 앱이 UserId.New()(v7)로 발급한다(ADR-006). EF가 값을 만들지 않게 한다.
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(Nickname.MaxLength)
            .HasConversion(n => n.Value, value => Nickname.From(value));

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .HasConversion(e => e.Value, value => Email.From(value));

        builder.Property(u => u.Gender)
            .HasColumnName("gender")
            .HasMaxLength(6)
            .HasConversion(g => GenderCode.ToCode(g), code => GenderCode.From(code));

        builder.Property(u => u.Birthdate)
            .HasColumnName("birthdate")
            .HasConversion(b => b!.Value, value => Birthdate.From(value));
    }
}
