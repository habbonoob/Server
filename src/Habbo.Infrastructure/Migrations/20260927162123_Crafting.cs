using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Crafting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crafting_recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AltarDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ProductDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    IsSecret = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crafting_recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crafting_recipes_item_definitions_AltarDefinitionId",
                        column: x => x.AltarDefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_crafting_recipes_item_definitions_ProductDefinitionId",
                        column: x => x.ProductDefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crafting_ingredients",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    DefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crafting_ingredients", x => new { x.RecipeId, x.DefinitionId });
                    table.ForeignKey(
                        name: "FK_crafting_ingredients_crafting_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "crafting_recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_crafting_ingredients_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crafting_ingredients_DefinitionId",
                table: "crafting_ingredients",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_crafting_recipes_AltarDefinitionId",
                table: "crafting_recipes",
                column: "AltarDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_crafting_recipes_Code",
                table: "crafting_recipes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crafting_recipes_ProductDefinitionId",
                table: "crafting_recipes",
                column: "ProductDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crafting_ingredients");

            migrationBuilder.DropTable(
                name: "crafting_recipes");
        }
    }
}
