using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomSettingsAndAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowFoodConsume",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNavigatorDynamicCategories",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowWalkThrough",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HiddenByBuildersClub",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "rooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "filtered_words",
                columns: table => new
                {
                    Word = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Replacement = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filtered_words", x => x.Word);
                });

            migrationBuilder.CreateTable(
                name: "room_bans",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_bans", x => new { x.RoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_room_bans_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_bans_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_filter_words",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    Word = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_filter_words", x => new { x.RoomId, x.Word });
                    table.ForeignKey(
                        name: "FK_room_filter_words_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_mutes",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_mutes", x => new { x.RoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_room_mutes_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_mutes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_bans_UserId",
                table: "room_bans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_mutes_UserId",
                table: "room_mutes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filtered_words");

            migrationBuilder.DropTable(
                name: "room_bans");

            migrationBuilder.DropTable(
                name: "room_filter_words");

            migrationBuilder.DropTable(
                name: "room_mutes");

            migrationBuilder.DropColumn(
                name: "AllowFoodConsume",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "AllowNavigatorDynamicCategories",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "AllowWalkThrough",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "HiddenByBuildersClub",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "rooms");
        }
    }
}
