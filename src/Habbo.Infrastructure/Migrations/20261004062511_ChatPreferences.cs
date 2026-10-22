using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatBubbleWidth",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 1); // normal, the client's default

            migrationBuilder.AddColumn<int>(
                name: "ChatFontSize",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatMode",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatScrollSpeed",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 1); // normal, the client's default

            migrationBuilder.AddColumn<int>(
                name: "OnlineIndicator",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatBubbleWidth",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "ChatFontSize",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "ChatMode",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "ChatScrollSpeed",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "OnlineIndicator",
                table: "user_preferences");
        }
    }
}
