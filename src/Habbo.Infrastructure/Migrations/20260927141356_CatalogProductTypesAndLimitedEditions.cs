using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogProductTypesAndLimitedEditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "catalog_products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "catalog_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "catalog_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LimitedSold",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LimitedStack",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // what was a product without a definition was a badge
            migrationBuilder.Sql("UPDATE catalog_products SET \"Type\" = 1 WHERE \"DefinitionId\" IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "catalog_products");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "catalog_products");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "catalog_products");

            migrationBuilder.DropColumn(
                name: "LimitedSold",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "LimitedStack",
                table: "catalog_offers");
        }
    }
}
