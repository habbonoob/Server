using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FurniBehaviours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Who may use an item is its behaviour's call, not the row's; every stored value was 0.
            migrationBuilder.DropColumn(
                name: "UsagePolicy",
                table: "user_items");

            migrationBuilder.AddColumn<string>(
                name: "CustomParams",
                table: "item_definitions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Interaction",
                table: "item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logic",
                table: "item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "furniture_basic");

            migrationBuilder.AddColumn<string>(
                name: "Visualization",
                table: "item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "furni_trophies",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    EngraverName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EngravedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_trophies", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_trophies_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_definition_directions",
                columns: table => new
                {
                    DefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_definition_directions", x => new { x.DefinitionId, x.Direction });
                    table.ForeignKey(
                        name: "FK_item_definition_directions_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_definition_states",
                columns: table => new
                {
                    DefinitionId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_definition_states", x => new { x.DefinitionId, x.State });
                    table.ForeignKey(
                        name: "FK_item_definition_states_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_trophies");

            migrationBuilder.DropTable(
                name: "item_definition_directions");

            migrationBuilder.DropTable(
                name: "item_definition_states");

            migrationBuilder.DropColumn(
                name: "CustomParams",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Interaction",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Logic",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "Visualization",
                table: "item_definitions");

            migrationBuilder.AddColumn<int>(
                name: "UsagePolicy",
                table: "user_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
