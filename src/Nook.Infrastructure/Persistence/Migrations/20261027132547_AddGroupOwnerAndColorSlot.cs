using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupOwnerAndColorSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 기존 행 채우기(개발 DB에 그룹이 있을 수 있다): 기본값으로 넣으면 FK·UNIQUE가 깨지므로
            // NULL 허용으로 추가 → 채우기 → NOT NULL 순서. 새 행은 앱이 항상 넣는다 — 기본값을 남기지 않는다(ADR-007 기준).
            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "color_slot",
                table: "group_members",
                type: "smallint",
                nullable: true);

            // 방장 = 가장 먼저 합류한 멤버(만든 회원은 created_at에 합류 — R-GR-08). 동점은 user_id.
            // 슬롯 = 합류 순서 0..3(정원 4 = 슬롯 4개라 겹치지 않는다).
            migrationBuilder.Sql("""
                UPDATE groups g SET owner_id = (
                    SELECT m.user_id FROM group_members m
                    WHERE m.group_id = g.id
                    ORDER BY m.joined_at, m.user_id
                    LIMIT 1);

                UPDATE group_members m SET color_slot = ranked.slot
                FROM (
                    SELECT group_id, user_id,
                           (row_number() OVER (PARTITION BY group_id ORDER BY joined_at, user_id) - 1)::smallint AS slot
                    FROM group_members) ranked
                WHERE m.group_id = ranked.group_id AND m.user_id = ranked.user_id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_id",
                table: "groups",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "color_slot",
                table: "group_members",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_owner_id",
                table: "groups",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ux_group_members_group_color_slot",
                table: "group_members",
                columns: new[] { "group_id", "color_slot" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_group_members_color_slot",
                table: "group_members",
                sql: "color_slot BETWEEN 0 AND 3");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_owner",
                table: "groups",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_owner",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_owner_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ux_group_members_group_color_slot",
                table: "group_members");

            migrationBuilder.DropCheckConstraint(
                name: "ck_group_members_color_slot",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "color_slot",
                table: "group_members");
        }
    }
}
