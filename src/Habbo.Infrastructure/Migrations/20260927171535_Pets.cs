using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Pets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pet_commands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    EnergyCost = table.Column<int>(type: "integer", nullable: false),
                    Posture = table.Column<string>(type: "text", nullable: true),
                    PostureSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_commands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pet_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Postures = table.Column<List<string>>(type: "text[]", nullable: false),
                    Gestures = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pet_palettes",
                columns: table => new
                {
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    PaletteId = table.Column<int>(type: "integer", nullable: false),
                    Sellable = table.Column<bool>(type: "boolean", nullable: false),
                    Rare = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_palettes", x => new { x.TypeId, x.PaletteId });
                    table.ForeignKey(
                        name: "FK_pet_palettes_pet_types_TypeId",
                        column: x => x.TypeId,
                        principalTable: "pet_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    PaletteId = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    Energy = table.Column<int>(type: "integer", nullable: false),
                    Nutrition = table.Column<int>(type: "integer", nullable: false),
                    VitalsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Respect = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Rotation = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pets_pet_types_TypeId",
                        column: x => x.TypeId,
                        principalTable: "pet_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pets_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pets_OwnerId_RoomId",
                table: "pets",
                columns: new[] { "OwnerId", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_pets_RoomId",
                table: "pets",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_TypeId",
                table: "pets",
                column: "TypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pet_commands");

            migrationBuilder.DropTable(
                name: "pet_palettes");

            migrationBuilder.DropTable(
                name: "pets");

            migrationBuilder.DropTable(
                name: "pet_types");
        }
    }
}
