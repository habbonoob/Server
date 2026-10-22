using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FurnitureAndProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ranking",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing rooms have no tags: default to an empty array so the column can be NOT NULL.
            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "rooms",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<int>(
                name: "TradeMode",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "Ranking",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "TradeMode",
                table: "rooms");
        }
    }
}
