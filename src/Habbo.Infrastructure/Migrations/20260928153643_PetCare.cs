using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PetCare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnyoneCanRide",
                table: "pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BreedingAllowed",
                table: "pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<int>>(
                name: "CustomParts",
                table: "pets",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "SaddleDefinitionId",
                table: "pets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Breed",
                table: "pet_palettes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ColorTag",
                table: "pet_palettes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Master",
                table: "pet_palettes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "pet_palettes",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.CreateTable(
                name: "pet_layers",
                columns: table => new
                {
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    LayerId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_layers", x => new { x.TypeId, x.LayerId });
                    table.ForeignKey(
                        name: "FK_pet_layers_pet_types_TypeId",
                        column: x => x.TypeId,
                        principalTable: "pet_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pet_packages",
                columns: table => new
                {
                    DefinitionId = table.Column<int>(type: "integer", nullable: false),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    Palettes = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_packages", x => x.DefinitionId);
                    table.ForeignKey(
                        name: "FK_pet_packages_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pet_packages_pet_types_TypeId",
                        column: x => x.TypeId,
                        principalTable: "pet_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pets_SaddleDefinitionId",
                table: "pets",
                column: "SaddleDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_pet_packages_TypeId",
                table: "pet_packages",
                column: "TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_pets_item_definitions_SaddleDefinitionId",
                table: "pets",
                column: "SaddleDefinitionId",
                principalTable: "item_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pets_item_definitions_SaddleDefinitionId",
                table: "pets");

            migrationBuilder.DropTable(
                name: "pet_layers");

            migrationBuilder.DropTable(
                name: "pet_packages");

            migrationBuilder.DropIndex(
                name: "IX_pets_SaddleDefinitionId",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "AnyoneCanRide",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "BreedingAllowed",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "CustomParts",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "SaddleDefinitionId",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "Breed",
                table: "pet_palettes");

            migrationBuilder.DropColumn(
                name: "ColorTag",
                table: "pet_palettes");

            migrationBuilder.DropColumn(
                name: "Master",
                table: "pet_palettes");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "pet_palettes");
        }
    }
}
