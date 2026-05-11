using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnmyAuto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gemini_api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gemini_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gemini_temperature = table.Column<float>(type: "real", nullable: false),
                    gemini_max_tokens = table.Column<int>(type: "integer", nullable: false),
                    content_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    content_tone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    default_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_scene_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_settings", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
