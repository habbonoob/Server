using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NavigatorLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "navigator_blocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    RequestCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BlockCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Text = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Query = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActionAllowed = table.Column<int>(type: "integer", nullable: false),
                    ViewMode = table.Column<int>(type: "integer", nullable: false),
                    MaxResults = table.Column<int>(type: "integer", nullable: false),
                    ForceClosed = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "navigator_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NodeName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    Automatic = table.Column<bool>(type: "boolean", nullable: false),
                    AutomaticCategoryKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GlobalCategoryKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StaffOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsEventCategory = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "navigator_tabs",
                columns: table => new
                {
                    SearchCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_tabs", x => x.SearchCode);
                });

            migrationBuilder.CreateTable(
                name: "navigator_quick_links",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    TabSearchCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SearchCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Filter = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Localization = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigator_quick_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_navigator_quick_links_navigator_tabs_TabSearchCode",
                        column: x => x.TabSearchCode,
                        principalTable: "navigator_tabs",
                        principalColumn: "SearchCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_navigator_blocks_RequestCode",
                table: "navigator_blocks",
                column: "RequestCode");

            migrationBuilder.CreateIndex(
                name: "IX_navigator_quick_links_TabSearchCode",
                table: "navigator_quick_links",
                column: "TabSearchCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "navigator_blocks");

            migrationBuilder.DropTable(
                name: "navigator_categories");

            migrationBuilder.DropTable(
                name: "navigator_quick_links");

            migrationBuilder.DropTable(
                name: "navigator_tabs");
        }
    }
}
