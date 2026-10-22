using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MonsterPlants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanBreed",
                table: "pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DiedAt",
                table: "pets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GrowthSkippedSeconds",
                table: "pets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rarity",
                table: "pets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WellUntil",
                table: "pets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "plant_seeds",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    PaletteId = table.Column<int>(type: "integer", nullable: false),
                    CustomParts = table.Column<List<int>>(type: "integer[]", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant_seeds", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_plant_seeds_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plant_traits",
                columns: table => new
                {
                    Tag = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant_traits", x => new { x.Tag, x.Value });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plant_seeds");

            migrationBuilder.DropTable(
                name: "plant_traits");

            migrationBuilder.DropColumn(
                name: "CanBreed",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "DiedAt",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "GrowthSkippedSeconds",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "Rarity",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "WellUntil",
                table: "pets");
        }
    }
}
