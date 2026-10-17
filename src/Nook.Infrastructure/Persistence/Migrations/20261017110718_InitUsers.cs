using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nickname = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_email", "email ~ '^[^@[:space:]]+@[^@[:space:]]+$'");
                    table.CheckConstraint("ck_users_nickname", "char_length(nickname) >= 2 AND nickname ~ '^[가-힣A-Za-z0-9]+$' AND nickname IS NFC NORMALIZED");
                });

            // lower() 표현식 UNIQUE는 HasIndex로 표현할 수 없어 SQL로 만든다(erd.md users). EF 모델 스냅샷에는 남지 않는다.
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_users_nickname_lower ON users (lower(nickname));");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
