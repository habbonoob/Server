using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Wired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "furni_wired",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    IntParams = table.Column<List<int>>(type: "integer[]", nullable: false),
                    StringParam = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    StuffIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    StuffIds2 = table.Column<List<int>>(type: "integer[]", nullable: false),
                    FurniSources = table.Column<List<int>>(type: "integer[]", nullable: false),
                    UserSources = table.Column<List<int>>(type: "integer[]", nullable: false),
                    VariableIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    DelayInPulses = table.Column<int>(type: "integer", nullable: false),
                    Quantifier = table.Column<int>(type: "integer", nullable: false),
                    IsFilter = table.Column<bool>(type: "boolean", nullable: false),
                    IsInvert = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_wired", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_wired_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_wired_snapshots",
                columns: table => new
                {
                    WiredItemId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Z = table.Column<double>(type: "double precision", nullable: false),
                    Rotation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_wired_snapshots", x => new { x.WiredItemId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_furni_wired_snapshots_furni_wired_WiredItemId",
                        column: x => x.WiredItemId,
                        principalTable: "furni_wired",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_furni_wired_snapshots_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_furni_wired_snapshots_ItemId",
                table: "furni_wired_snapshots",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_wired_snapshots");

            migrationBuilder.DropTable(
                name: "furni_wired");
        }
    }
}
