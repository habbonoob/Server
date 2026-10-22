using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogPageOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "catalog_page_offers",
                columns: table => new
                {
                    OffersId = table.Column<int>(type: "integer", nullable: false),
                    PagesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_page_offers", x => new { x.OffersId, x.PagesId });
                    table.ForeignKey(
                        name: "FK_catalog_page_offers_catalog_offers_OffersId",
                        column: x => x.OffersId,
                        principalTable: "catalog_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_page_offers_catalog_pages_PagesId",
                        column: x => x.PagesId,
                        principalTable: "catalog_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_page_offers_OffersId",
                table: "catalog_page_offers",
                column: "OffersId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_page_offers_PagesId",
                table: "catalog_page_offers",
                column: "PagesId");

            // The link table is filled from the page each offer already pointed at before the
            // old column is dropped; otherwise every existing listing would be lost, and the
            // catalog seeder skips itself once pages exist so nothing would put them back.
            migrationBuilder.Sql("""
                INSERT INTO catalog_page_offers ("OffersId", "PagesId")
                SELECT "Id", "PageId" FROM catalog_offers;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_offers_catalog_pages_PageId",
                table: "catalog_offers");

            migrationBuilder.DropIndex(
                name: "IX_catalog_offers_PageId",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "catalog_offers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_page_offers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "PageId",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offers_PageId",
                table: "catalog_offers",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_offers_catalog_pages_PageId",
                table: "catalog_offers",
                column: "PageId",
                principalTable: "catalog_pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
