using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGenderBirthdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birthdate",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "none");

            // 기존 회원은 'none'(밝히지 않음)으로 채운 뒤 기본값을 지운다 — 새 행은 앱이 항상 넣는다(시각 기본값과 같은 기준, ADR-007).
            // 남겨 두면 값을 빠뜨린 INSERT가 조용히 'none'이 된다.
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN gender DROP DEFAULT;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_birthdate",
                table: "users",
                sql: "birthdate >= DATE '1900-01-01'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_gender",
                table: "users",
                sql: "gender IN ('female', 'male', 'none')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_birthdate",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_gender",
                table: "users");

            migrationBuilder.DropColumn(
                name: "birthdate",
                table: "users");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "users");
        }
    }
}
