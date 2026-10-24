using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_reviews_version",
                table: "reviews",
                sql: "version >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reviews_version",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "version",
                table: "reviews");
        }
    }
}
