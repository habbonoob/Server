using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AchievementBadgePerLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_achievements_BadgeCode",
                table: "achievements");

            migrationBuilder.RenameColumn(
                name: "BadgeCode",
                table: "achievements",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "BadgeCode",
                table: "achievement_levels",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeCode",
                table: "achievement_levels");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "achievements",
                newName: "BadgeCode");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_BadgeCode",
                table: "achievements",
                column: "BadgeCode",
                unique: true);
        }
    }
}
