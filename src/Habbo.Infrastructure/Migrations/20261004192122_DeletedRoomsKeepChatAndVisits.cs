using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeletedRoomsKeepChatAndVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_room_chat_lines_rooms_RoomId",
                table: "room_chat_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_room_visits_rooms_RoomId",
                table: "room_visits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_room_chat_lines_rooms_RoomId",
                table: "room_chat_lines",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_room_visits_rooms_RoomId",
                table: "room_visits",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
