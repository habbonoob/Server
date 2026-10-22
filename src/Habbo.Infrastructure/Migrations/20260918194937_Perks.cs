using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Perks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "perks",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MinimumRank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perks", x => x.Code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "perks");
        }
    }
}
