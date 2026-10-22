using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WiredContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wired_contracts",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    YouGive = table.Column<string>(type: "jsonb", nullable: true),
                    YouGet = table.Column<string>(type: "jsonb", nullable: true),
                    PaymentMode = table.Column<short>(type: "smallint", nullable: false),
                    ReceiveText = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    LayoutType = table.Column<string>(type: "text", nullable: false),
                    RewardCategory = table.Column<short>(type: "smallint", nullable: false),
                    ShowDialog = table.Column<bool>(type: "boolean", nullable: false),
                    RewardText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wired_contracts", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_wired_contracts_user_items_ItemId",
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
                name: "wired_contracts");
        }
    }
}
