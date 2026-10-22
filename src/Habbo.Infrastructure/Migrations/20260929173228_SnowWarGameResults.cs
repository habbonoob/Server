using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SnowWarGameResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "snowwar_game_results",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    FieldType = table.Column<int>(type: "integer", nullable: false),
                    Team = table.Column<int>(type: "integer", nullable: false),
                    Won = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Kills = table.Column<int>(type: "integer", nullable: false),
                    Deaths = table.Column<int>(type: "integer", nullable: false),
                    SnowballHits = table.Column<int>(type: "integer", nullable: false),
                    SnowballHitsTaken = table.Column<int>(type: "integer", nullable: false),
                    SnowballsThrown = table.Column<int>(type: "integer", nullable: false),
                    SnowballsCreated = table.Column<int>(type: "integer", nullable: false),
                    SnowballsFromMachine = table.Column<int>(type: "integer", nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snowwar_game_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_snowwar_game_results_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_snowwar_game_results_PlayedAt",
                table: "snowwar_game_results",
                column: "PlayedAt");

            migrationBuilder.CreateIndex(
                name: "IX_snowwar_game_results_UserId_PlayedAt",
                table: "snowwar_game_results",
                columns: new[] { "UserId", "PlayedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snowwar_game_results");
        }
    }
}
