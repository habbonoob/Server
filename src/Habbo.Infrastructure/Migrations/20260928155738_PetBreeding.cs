using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PetBreeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pet_rarity_categories",
                columns: table => new
                {
                    BabyTypeId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Chance = table.Column<double>(type: "double precision", nullable: false),
                    ChancePerLevel = table.Column<double>(type: "double precision", nullable: false),
                    Palettes = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_rarity_categories", x => new { x.BabyTypeId, x.Rank });
                    table.ForeignKey(
                        name: "FK_pet_rarity_categories_pet_types_BabyTypeId",
                        column: x => x.BabyTypeId,
                        principalTable: "pet_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pet_rarity_categories");
        }
    }
}
