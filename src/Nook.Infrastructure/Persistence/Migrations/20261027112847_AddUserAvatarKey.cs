using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_key",
                table: "users",
                type: "character varying(56)",
                maxLength: 56,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_avatar_key",
                table: "users",
                sql: "btrim(avatar_key) <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_avatar_key",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar_key",
                table: "users");
        }
    }
}
