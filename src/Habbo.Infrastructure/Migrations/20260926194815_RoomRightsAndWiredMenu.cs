using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomRightsAndWiredMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FloorItemLimit",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 2500);

            migrationBuilder.AddColumn<int>(
                name: "WallItemLimit",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<int>(
                name: "WiredModifyPermissionMask",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WiredReadPermissionMask",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WiredTimezone",
                table: "rooms",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "room_rights",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_rights", x => new { x.RoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_room_rights_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_rights_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wired_room_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<byte>(type: "smallint", nullable: false),
                    Source = table.Column<byte>(type: "smallint", nullable: false),
                    Message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wired_room_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wired_room_logs_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_rights_UserId",
                table: "room_rights",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_wired_room_logs_RoomId_Id",
                table: "wired_room_logs",
                columns: new[] { "RoomId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_rights");

            migrationBuilder.DropTable(
                name: "wired_room_logs");

            migrationBuilder.DropColumn(
                name: "FloorItemLimit",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WallItemLimit",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WiredModifyPermissionMask",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WiredReadPermissionMask",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "WiredTimezone",
                table: "rooms");
        }
    }
}
