using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewNavigatorPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lifted_rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    AreaId = table.Column<int>(type: "integer", nullable: false),
                    Image = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Caption = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lifted_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lifted_rooms_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "navigator_collapsed_categories",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_collapsed_categories", x => new { x.UserId, x.Category });
                    table.ForeignKey(
                        name: "FK_navigator_collapsed_categories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "navigator_saved_searches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SearchCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Filter = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_saved_searches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_navigator_saved_searches_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "navigator_view_modes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SearchCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ViewMode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_view_modes", x => new { x.UserId, x.SearchCode });
                    table.ForeignKey(
                        name: "FK_navigator_view_modes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "navigator_window_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    LeftPaneHidden = table.Column<bool>(type: "boolean", nullable: false),
                    ResultsMode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_window_preferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_navigator_window_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lifted_rooms_RoomId",
                table: "lifted_rooms",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_navigator_saved_searches_UserId",
                table: "navigator_saved_searches",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lifted_rooms");

            migrationBuilder.DropTable(
                name: "navigator_collapsed_categories");

            migrationBuilder.DropTable(
                name: "navigator_saved_searches");

            migrationBuilder.DropTable(
                name: "navigator_view_modes");

            migrationBuilder.DropTable(
                name: "navigator_window_preferences");
        }
    }
}
