using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ItemGroupable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Groupable",
                table: "user_items");

            // Defaults to true so definitions seeded before this column existed start stacking:
            // the furniture seeder skips itself once any definition is present, so nothing would
            // otherwise go back and set them.
            migrationBuilder.AddColumn<bool>(
                name: "Groupable",
                table: "item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Groupable",
                table: "item_definitions");

            migrationBuilder.AddColumn<bool>(
                name: "Groupable",
                table: "user_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
