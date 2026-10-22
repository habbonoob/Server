using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AvatarEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avatar_effects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Library = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avatar_effects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_avatar_effects_Library",
                table: "avatar_effects",
                column: "Library");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avatar_effects");
        }
    }
}
