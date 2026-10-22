using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RentableSpaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rentable_space_rentals",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    RenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RentedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentable_space_rentals", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_rentable_space_rentals_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rentable_space_rentals_users_RenterId",
                        column: x => x.RenterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rentable_space_rentals_RenterId",
                table: "rentable_space_rentals",
                column: "RenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rentable_space_rentals");
        }
    }
}
