using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Bots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultName = table.Column<string>(type: "text", nullable: false),
                    DefaultMotto = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Skills = table.Column<List<int>>(type: "integer[]", nullable: false),
                    Beverages = table.Column<List<int>>(type: "integer[]", nullable: false),
                    LogsVisitors = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bot_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Motto = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    Figure = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Rotation = table.Column<int>(type: "integer", nullable: false),
                    ChatLines = table.Column<List<string>>(type: "text[]", nullable: false),
                    AutoChat = table.Column<bool>(type: "boolean", nullable: false),
                    ChatDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    MixSentences = table.Column<bool>(type: "boolean", nullable: false),
                    WalksRandomly = table.Column<bool>(type: "boolean", nullable: false),
                    DanceStyle = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bots_bot_types_TypeId",
                        column: x => x.TypeId,
                        principalTable: "bot_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bots_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bots_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bot_types_Code",
                table: "bot_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bots_OwnerId_RoomId",
                table: "bots",
                columns: new[] { "OwnerId", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_bots_RoomId",
                table: "bots",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_bots_TypeId",
                table: "bots",
                column: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bots");

            migrationBuilder.DropTable(
                name: "bot_types");
        }
    }
}
