using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CurrencySeasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency_season_resets",
                columns: table => new
                {
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SeasonEndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_season_resets", x => new { x.Type, x.SeasonEndedAt });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currency_season_resets");
        }
    }
}
