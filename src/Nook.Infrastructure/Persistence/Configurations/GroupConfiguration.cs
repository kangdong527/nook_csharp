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

        // 받는 쿼리: erd.md groups Q2·Q3·Q5
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

        // 받는 쿼리: erd.md groups Q1·Q4 (R-GR-06 중복 방지)
        builder.HasIndex(g => g.InviteCode)
            .IsUnique()
            .HasDatabaseName("ux_groups_invite_code");

        builder.Property(g => g.CreatedAt).HasColumnName("created_at");

        // R-GR-11 방장. RESTRICT — 방장 회원을 지우려면 먼저 그룹에서 나가 방장이 넘어가야 한다(PF-02 절차, ADR-012).
        // 원본의 CASCADE는 방장 탈퇴 = 남은 멤버의 그룹 삭제였다.
        builder.Property(g => g.OwnerId).HasColumnName("owner_id");
        builder.HasOne<User>().WithMany()
            .HasForeignKey(g => g.OwnerId)
            .HasConstraintName("fk_groups_owner")
            .OnDelete(DeleteBehavior.Restrict);

        // 받는 쿼리: 회원 삭제(PF-02) 때 fk_groups_owner 검사 — 없으면 groups 전체를 읽는다(erd.md users "FK 들어오는 것")
        builder.HasIndex(g => g.OwnerId).HasDatabaseName("ix_groups_owner_id");

        builder.OwnsMany(g => g.Members, members =>
        {
            members.ToTable("group_members", table =>
                table.HasCheckConstraint("ck_group_members_color_slot", $"color_slot BETWEEN 0 AND {Group.ColorSlotCount - 1}"));

            // 부모 ID는 Domain에 없다(D2) → 섀도 속성. 그룹 안의 식별자는 UserId(R-GR-07)
            members.WithOwner().HasForeignKey("GroupId").HasConstraintName("fk_group_members_groups");
            members.Property<GroupId>("GroupId").HasColumnName("group_id");

            // 받는 쿼리: erd.md group_members Q1·Q3 (R-GR-07 중복 방지)
            members.HasKey("GroupId", nameof(GroupMember.UserId)).HasName("pk_group_members");

            members.Property(m => m.UserId).HasColumnName("user_id");
            members.Property(m => m.JoinedAt).HasColumnName("joined_at");
            members.Property(m => m.ColorSlot).HasColumnName("color_slot");

            // R-GR-12 그룹 안 색 중복 금지 — 합류는 그룹 행 잠금 안에서 고르므로(D6) 이건 이중화
            members.HasIndex("GroupId", nameof(GroupMember.ColorSlot))
                .IsUnique()
                .HasDatabaseName("ux_group_members_group_color_slot");

            // 받는 쿼리: erd.md group_members Q2·Q4
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
