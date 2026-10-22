using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Gifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "furni_gifts",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    ExtraData = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LimitedSerial = table.Column<int>(type: "integer", nullable: true),
                    LimitedStack = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_gifts", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_gifts_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_furni_gifts_users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_furni_gifts_SenderId",
                table: "furni_gifts",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_gifts");
        }
    }
}
