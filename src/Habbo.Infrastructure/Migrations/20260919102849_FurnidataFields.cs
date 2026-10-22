using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FurnidataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanStackOn",
                table: "item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "item_definitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DefaultDirection",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "item_definitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FurniLine",
                table: "item_definitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "item_definitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Rare",
                table: "item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Recyclable",
                table: "item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpecialType",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Tradeable",
                table: "item_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanStackOn",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "DefaultDirection",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "FurniLine",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Rare",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Recyclable",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "SpecialType",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Tradeable",
                table: "item_definitions");
        }
    }
}
