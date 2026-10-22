using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LoginSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InfoFeedEnabled",
                table: "user_preferences",
                type: "boolean",
                nullable: false,
                // The client's own default is on, and so is the property's, so rows that already
                // exist keep the feed rather than silently losing it.
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "call_for_help_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_call_for_help_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "call_for_help_topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Consequence = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_call_for_help_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_call_for_help_topics_call_for_help_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "call_for_help_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_call_for_help_topics_CategoryId",
                table: "call_for_help_topics",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "call_for_help_topics");

            migrationBuilder.DropTable(
                name: "call_for_help_categories");

            migrationBuilder.DropColumn(
                name: "InfoFeedEnabled",
                table: "user_preferences");
        }
    }
}
