using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventAndPromotedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promoted_room_categories",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    RoomIds = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promoted_room_categories", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "room_event_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_event_categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_events_CategoryId",
                table: "room_events",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_room_events_room_event_categories_CategoryId",
                table: "room_events",
                column: "CategoryId",
                principalTable: "room_event_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_room_events_room_event_categories_CategoryId",
                table: "room_events");

            migrationBuilder.DropTable(
                name: "promoted_room_categories");

            migrationBuilder.DropTable(
                name: "room_event_categories");

            migrationBuilder.DropIndex(
                name: "IX_room_events_CategoryId",
                table: "room_events");
        }
    }
}
