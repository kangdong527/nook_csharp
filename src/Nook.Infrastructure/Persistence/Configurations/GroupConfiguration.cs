using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Groups;
using Nook.Domain.Users;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 groups + group_members. GroupMember는 애그리거트 내부 엔티티라 OwnsMany로 Group과 함께 로딩·저장된다.
internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups", table =>
        {
            table.HasCheckConstraint("ck_groups_invite_code", "invite_code ~ '^[A-HJ-NP-Z2-9]{6}$'");
            table.HasCheckConstraint("ck_groups_name",
                "char_length(name) >= 2 AND name = btrim(name) AND name ~ '^[가-힣A-Za-z0-9 ]+$' AND name IS NFC NORMALIZED");
        });

        builder.HasKey(g => g.Id).HasName("pk_groups");

        builder.Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(GroupName.MaxLength)
            .HasConversion(n => n.Value, value => GroupName.From(value));

        builder.Property(g => g.InviteCode)
            .HasColumnName("invite_code")
            .HasMaxLength(InviteCode.Length)
            .HasConversion(c => c.Value, value => InviteCode.From(value));

        builder.HasIndex(g => g.InviteCode)
            .IsUnique()
            .HasDatabaseName("ux_groups_invite_code");

        builder.Property(g => g.CreatedAt).HasColumnName("created_at");

        builder.OwnsMany(g => g.Members, members =>
        {
            members.ToTable("group_members");

            // 부모 ID는 Domain에 없다(D2) → 섀도 속성. 그룹 안의 식별자는 UserId(R-GR-07)
            members.WithOwner().HasForeignKey("GroupId").HasConstraintName("fk_group_members_groups");
            members.Property<GroupId>("GroupId").HasColumnName("group_id");

            members.HasKey("GroupId", nameof(GroupMember.UserId)).HasName("pk_group_members");

            members.Property(m => m.UserId).HasColumnName("user_id");
            members.Property(m => m.JoinedAt).HasColumnName("joined_at");

            members.HasIndex(m => m.UserId).HasDatabaseName("ix_group_members_user_id");

            // 탈퇴(PF-02)는 Group.RemoveMember를 먼저 거쳐야 한다 → RESTRICT(R-GR-09, ADR-004)
            members.HasOne<User>().WithMany()
                .HasForeignKey(m => m.UserId)
                .HasConstraintName("fk_group_members_users")
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Navigation(g => g.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
