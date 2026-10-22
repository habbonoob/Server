using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Recycler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OfferId",
                table: "furni_gifts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "PrizeDefinitionId",
                table: "furni_gifts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recycler_prize_levels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ProbabilityDenominator = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recycler_prize_levels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recycler_uses",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastRecycledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recycler_uses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_recycler_uses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recycler_prizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    LevelId = table.Column<int>(type: "integer", nullable: false),
                    DefinitionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recycler_prizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recycler_prizes_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recycler_prizes_recycler_prize_levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "recycler_prize_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recycler_prizes_DefinitionId",
                table: "recycler_prizes",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_recycler_prizes_LevelId",
                table: "recycler_prizes",
                column: "LevelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recycler_prizes");

            migrationBuilder.DropTable(
                name: "recycler_uses");

            migrationBuilder.DropTable(
                name: "recycler_prize_levels");

            migrationBuilder.DropColumn(
                name: "PrizeDefinitionId",
                table: "furni_gifts");

            migrationBuilder.AlterColumn<int>(
                name: "OfferId",
                table: "furni_gifts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
