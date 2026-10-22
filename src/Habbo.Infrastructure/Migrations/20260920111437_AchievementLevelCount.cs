using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AchievementLevelCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "achievements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "LevelCount",
                table: "achievements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Achievements already on record know how many levels they have: the rows they have.
            // A capture can later raise this for ones whose later levels are not on record.
            migrationBuilder.Sql("""
                UPDATE achievements a
                SET "LevelCount" = (
                    SELECT COUNT(*) FROM achievement_levels l WHERE l."AchievementId" = a."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevelCount",
                table: "achievements");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "achievements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
