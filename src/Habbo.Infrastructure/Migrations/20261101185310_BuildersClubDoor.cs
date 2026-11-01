using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BuildersClubDoor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoorBeforeBuildersClub",
                table: "rooms",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoorBeforeBuildersClub",
                table: "rooms");
        }
    }
}
