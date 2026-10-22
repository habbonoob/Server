using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlayerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DiscordAllowJoining",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DiscordHideInHiddenRooms",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DiscordShareActivity",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DiscordShowHabbo",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiscordVersion",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "user_filter_words",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_filter_words", x => new { x.UserId, x.Word });
                    table.ForeignKey(
                        name: "FK_user_filter_words_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_filter_words");

            migrationBuilder.DropColumn(
                name: "DiscordAllowJoining",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "DiscordHideInHiddenRooms",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "DiscordShareActivity",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "DiscordShowHabbo",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "DiscordVersion",
                table: "user_preferences");
        }
    }
}
