using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Habbo.Domain.Users;

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GenderRankAndClubLevelEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Sex",
                table: "users",
                newName: "Gender");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "users",
                newName: "Sex");
        }
    }
}
