using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WithdrawalKeepsReviewsAndLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_users",
                table: "likes");

            migrationBuilder.DropForeignKey(
                name: "fk_reviews_users",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "pk_likes",
                table: "likes");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "reviews",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "likes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // 기존 좋아요 채우기: NULL 허용으로 추가 → 행마다 다른 값 → NOT NULL. 기본값 0-GUID로 넣으면 새 PK가 충돌한다.
            // PostgreSQL 17엔 uuidv7()이 없어 기존 행만 v4(gen_random_uuid). 새 행은 앱이 v7(GuidV7Generator·LikeRepository).
            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "likes",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE likes SET id = gen_random_uuid();");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "likes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_likes",
                table: "likes",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ux_likes_review_user",
                table: "likes",
                columns: new[] { "review_id", "user_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_users",
                table: "likes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_users",
                table: "reviews",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_users",
                table: "likes");

            migrationBuilder.DropForeignKey(
                name: "fk_reviews_users",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "pk_likes",
                table: "likes");

            migrationBuilder.DropIndex(
                name: "ux_likes_review_user",
                table: "likes");

            migrationBuilder.DropColumn(
                name: "id",
                table: "likes");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "reviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "likes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_likes",
                table: "likes",
                columns: new[] { "review_id", "user_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_likes_users",
                table: "likes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_users",
                table: "reviews",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
