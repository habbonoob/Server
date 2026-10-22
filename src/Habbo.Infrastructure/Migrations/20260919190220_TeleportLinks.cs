using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeleportLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "furni_teleport_links",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    LinkedItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_teleport_links", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_teleport_links_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_furni_teleport_links_user_items_LinkedItemId",
                        column: x => x.LinkedItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_furni_teleport_links_LinkedItemId",
                table: "furni_teleport_links",
                column: "LinkedItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_teleport_links");
        }
    }
}
