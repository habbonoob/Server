using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WiredRoomLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StringParam",
                table: "furni_wired",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.CreateTable(
                name: "wired_link_networks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wired_link_networks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wired_link_networks_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wired_link_endpoints",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    NetworkId = table.Column<int>(type: "integer", nullable: true),
                    ReceiveLocal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wired_link_endpoints", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_wired_link_endpoints_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wired_link_endpoints_wired_link_networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "wired_link_networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wired_link_endpoints_NetworkId",
                table: "wired_link_endpoints",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_wired_link_networks_OwnerId_Name",
                table: "wired_link_networks",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wired_link_endpoints");

            migrationBuilder.DropTable(
                name: "wired_link_networks");

            migrationBuilder.AlterColumn<string>(
                name: "StringParam",
                table: "furni_wired",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000);
        }
    }
}
