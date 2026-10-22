using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomFurnitureBehaviours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "furni_dimmer_presets",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    PresetId = table.Column<int>(type: "integer", nullable: false),
                    EffectType = table.Column<int>(type: "integer", nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Brightness = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_dimmer_presets", x => new { x.ItemId, x.PresetId });
                    table.ForeignKey(
                        name: "FK_furni_dimmer_presets_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_post_its",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_post_its", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_post_its_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_stack_heights",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    MultiWalk = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_stack_heights", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_stack_heights_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_dimmer_presets");

            migrationBuilder.DropTable(
                name: "furni_post_its");

            migrationBuilder.DropTable(
                name: "furni_stack_heights");
        }
    }
}
