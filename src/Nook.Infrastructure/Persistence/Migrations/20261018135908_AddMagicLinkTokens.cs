using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicLinkTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "magic_link_tokens",
                columns: table => new
                {
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_magic_link_tokens", x => x.token_hash);
                    table.CheckConstraint("ck_magic_link_tokens_consumed", "consumed_at IS NULL OR consumed_at >= created_at");
                    table.CheckConstraint("ck_magic_link_tokens_expires", "expires_at > created_at");
                    table.CheckConstraint("ck_magic_link_tokens_token_hash", "octet_length(token_hash) = 32");
                });

            // EF 모델로 표현할 수 없는 인덱스 — 모델 스냅샷에 남지 않으므로 바꾸거나 지울 때도 SQL로 한다(erd.md).
            // 받는 쿼리: erd.md magic_link_tokens Q2(등호 + created_at 범위)·Q3(앞 열만)
            // Down은 DropTable이 인덱스까지 지우므로 따로 두지 않는다.
            migrationBuilder.Sql(
                "CREATE INDEX ix_magic_link_tokens_email_lower_created ON magic_link_tokens (lower(email), created_at);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "magic_link_tokens");
        }
    }
}
