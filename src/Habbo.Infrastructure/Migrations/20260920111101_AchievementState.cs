using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AchievementState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "State",
                table: "achievements",
                type: "smallint",
                nullable: false,
                // AchievementState.Active. Zero is not a state the client knows, and the CLR
                // default would hand every existing achievement one.
                defaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "achievements");
        }
    }
}
