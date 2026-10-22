using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllInRoomMuted",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ChatBubbleWidth",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatFloodSensitivity",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatHearRange",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatMode",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChatScrollSpeed",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FloorType",
                table: "rooms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsStaffPick",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Landscape",
                table: "rooms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Wallpaper",
                table: "rooms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WhoCanBan",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhoCanKick",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhoCanMute",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllInRoomMuted",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ChatBubbleWidth",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ChatFloodSensitivity",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ChatHearRange",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ChatMode",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ChatScrollSpeed",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "FloorType",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "IsStaffPick",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "Landscape",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "Wallpaper",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WhoCanBan",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WhoCanKick",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WhoCanMute",
                table: "rooms");
        }
    }
}
