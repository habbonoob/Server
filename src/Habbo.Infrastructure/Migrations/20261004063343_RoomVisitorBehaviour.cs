using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomVisitorBehaviour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IdleAutokickEnabled",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: true); // the client's default

            migrationBuilder.AddColumn<int>(
                name: "IdleAutokickTimeoutSeconds",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IdleSleepEnabled",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: true); // the client's default

            migrationBuilder.AddColumn<int>(
                name: "IdleSleepTimeoutSeconds",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LeaveOnDoorTile",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: true); // the client's default

            migrationBuilder.AddColumn<bool>(
                name: "MuteAllPets",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdleAutokickEnabled",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "IdleAutokickTimeoutSeconds",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "IdleSleepEnabled",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "IdleSleepTimeoutSeconds",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "LeaveOnDoorTile",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "MuteAllPets",
                table: "rooms");
        }
    }
}
