using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_definitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ClassName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    XDim = table.Column<int>(type: "integer", nullable: false),
                    YDim = table.Column<int>(type: "integer", nullable: false),
                    StackHeight = table.Column<double>(type: "double precision", nullable: false),
                    CanWalk = table.Column<bool>(type: "boolean", nullable: false),
                    CanSit = table.Column<bool>(type: "boolean", nullable: false),
                    CanLay = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ExtraData = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    X = table.Column<int>(type: "integer", nullable: true),
                    Y = table.Column<int>(type: "integer", nullable: true),
                    Z = table.Column<double>(type: "double precision", nullable: true),
                    Rotation = table.Column<int>(type: "integer", nullable: true),
                    WallPosition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_items_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_items_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_items_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_definitions_ClassName",
                table: "item_definitions",
                column: "ClassName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_items_DefinitionId",
                table: "user_items",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_items_OwnerId",
                table: "user_items",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_user_items_RoomId",
                table: "user_items",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_items");

            migrationBuilder.DropTable(
                name: "item_definitions");
        }
    }
}
