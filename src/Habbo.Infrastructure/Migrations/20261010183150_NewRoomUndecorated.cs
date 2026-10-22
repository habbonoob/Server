using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewRoomUndecorated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // rooms still on the old invented default (floor 101, wallpaper 201, landscape 1.1), which no player could
            // have chosen, become undecorated as on Habbo: no floor or wallpaper, landscape 0.0
            migrationBuilder.Sql(
                """
                UPDATE rooms SET "FloorType" = '', "Wallpaper" = '', "Landscape" = '0.0'
                WHERE "FloorType" = '101' AND "Wallpaper" = '201' AND "Landscape" = '1.1';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
