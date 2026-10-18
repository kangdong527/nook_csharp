using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.CheckConstraint("ck_groups_invite_code", "invite_code ~ '^[A-HJ-NP-Z2-9]{6}$'");
                    table.CheckConstraint("ck_groups_name", "char_length(name) >= 2 AND name = btrim(name) AND name ~ '^[가-힣A-Za-z0-9 ]+$' AND name IS NFC NORMALIZED");
                });

            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kakao_place_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_places", x => x.id);
                    table.CheckConstraint("ck_places_kakao_place_id", "btrim(kakao_place_id) <> ''");
                    table.CheckConstraint("ck_places_latitude", "latitude >= -90 AND latitude <= 90");
                    table.CheckConstraint("ck_places_longitude", "longitude >= -180 AND longitude <= 180");
                    table.CheckConstraint("ck_places_name", "btrim(name) <> ''");
                });

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

            migrationBuilder.CreateTable(
                name: "bookmarks",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookmarks", x => new { x.user_id, x.place_id });
                    table.ForeignKey(
                        name: "fk_bookmarks_places",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookmarks_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => new { x.group_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_group_members_groups",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_members_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    review_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.CheckConstraint("ck_reviews_content", "char_length(btrim(content)) >= 10");
                    table.ForeignKey(
                        name: "fk_reviews_places",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "likes",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_likes", x => new { x.review_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_likes_reviews",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_likes_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(56)", maxLength: 56, nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_images", x => x.id);
                    table.CheckConstraint("ck_review_images_sort_order", "sort_order BETWEEN 0 AND 2");
                    table.CheckConstraint("ck_review_images_storage_key", "btrim(storage_key) <> ''");
                    table.ForeignKey(
                        name: "fk_review_images_reviews",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_members_user_id",
                table: "group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_groups_invite_code",
                table: "groups",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_likes_user_id",
                table: "likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_places_kakao_place_id",
                table: "places",
                column: "kakao_place_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_review_images_storage_key",
                table: "review_images",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviews_place_created",
                table: "reviews",
                columns: new[] { "place_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_reviews_user_place_date",
                table: "reviews",
                columns: new[] { "user_id", "place_id", "review_date" },
                unique: true);

            // EF 모델로 표현할 수 없는 제약 — 모델 스냅샷에 남지 않으므로 바꾸거나 지울 때도 SQL로 한다(erd.md).
            // lower() 표현식 UNIQUE: UNIQUE 제약은 식을 받지 못해 인덱스로 만든다.
            // 받는 쿼리: erd.md users Q3 (R-US-02) / Q4 (R-US-03)
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_users_nickname_lower ON users (lower(nickname));");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email));");

            // 순서 맞바꾸기(0↔1)의 중간 중복을 허용하고 커밋 시점에 검사. 인덱스는 DEFERRABLE을 받지 못해 제약으로 만든다.
            // 받는 쿼리: erd.md review_images Q1·Q3 (앞 컬럼 review_id, R-RV-02 순서 중복 방지)
            // EF 모델에는 알리지 않는다(ADR-008). EF는 이 제약을 모른 채 맞바꾸기 UPDATE를 기본 키 순으로 보내고(D4-1 SQL 로그),
            // 어느 순서든 첫 UPDATE 직후 같은 sort_order가 두 행이 되므로 순서가 아니라 커밋 시점 검사(DB)가 정합성을 맡는다.
            migrationBuilder.Sql(
                "ALTER TABLE review_images ADD CONSTRAINT ux_review_images_review_sort_order " +
                "UNIQUE (review_id, sort_order) DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookmarks");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "likes");

            migrationBuilder.DropTable(
                name: "review_images");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "places");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
