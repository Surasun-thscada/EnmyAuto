using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnmyAuto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    quota_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "storyboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    script_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storyboards", x => x.id);
                    table.ForeignKey(
                        name: "fk_storyboards_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tiktok_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    showcase_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tiktok_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_tiktok_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storyboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    file_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_assets_storyboards_storyboard_id",
                        column: x => x.storyboard_id,
                        principalTable: "storyboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auto_post_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storyboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tiktok_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    tiktok_video_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auto_post_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_auto_post_campaigns_storyboards_storyboard_id",
                        column: x => x.storyboard_id,
                        principalTable: "storyboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auto_post_campaigns_tiktok_accounts_tiktok_account_id",
                        column: x => x.tiktok_account_id,
                        principalTable: "tiktok_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auto_post_campaigns_storyboard_id",
                table: "auto_post_campaigns",
                column: "storyboard_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_account_scheduled",
                table: "auto_post_campaigns",
                columns: new[] { "tiktok_account_id", "scheduled_time" });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_status",
                table: "auto_post_campaigns",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_storyboard_type",
                table: "media_assets",
                columns: new[] { "storyboard_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_storyboards_user_status",
                table: "storyboards",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tiktok_accounts_user_id",
                table: "tiktok_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auto_post_campaigns");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "tiktok_accounts");

            migrationBuilder.DropTable(
                name: "storyboards");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
