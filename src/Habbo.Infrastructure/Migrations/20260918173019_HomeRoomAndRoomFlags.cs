using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HomeRoomAndRoomFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HomeRoomId",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPets",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OfficialPicRef",
                table: "rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOwner",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeRoomId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AllowPets",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "OfficialPicRef",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ShowOwner",
                table: "rooms");
        }
    }
}
