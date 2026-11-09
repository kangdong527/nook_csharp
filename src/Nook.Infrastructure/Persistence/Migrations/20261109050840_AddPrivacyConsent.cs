using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "optional_consented_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "privacy_consented_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_policy_version",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_privacy_consent",
                table: "users",
                sql: "(privacy_consented_at IS NULL) = (privacy_policy_version IS NULL)");

            // R-US-06: 성별·생년월일은 선택 동의가 있을 때만. NOT VALID — 동의 기록 도입 전 행(로컬 개발 데이터)은 검사하지 않고
            // 새로 쓰는 행부터 강제한다. HasCheckConstraint 로 표현할 수 없어 SQL(ADR-008). 기존 행을 정리하면 VALIDATE CONSTRAINT 로 승격
            migrationBuilder.Sql(
                "ALTER TABLE users ADD CONSTRAINT ck_users_optional_consent " +
                "CHECK ((gender IS NULL AND birthdate IS NULL) OR optional_consented_at IS NOT NULL) NOT VALID;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE users DROP CONSTRAINT ck_users_optional_consent;");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_privacy_consent",
                table: "users");

            migrationBuilder.DropColumn(
                name: "optional_consented_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "privacy_consented_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "privacy_policy_version",
                table: "users");

            // 선택이 된 뒤 생긴 NULL 을 none(밝히지 않음)으로 — EF 기본값 "" 는 ck_users_gender 를 어겨 되돌리기가 실패한다
            migrationBuilder.Sql("UPDATE users SET gender = 'none' WHERE gender IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6,
                oldNullable: true);
        }
    }
}
