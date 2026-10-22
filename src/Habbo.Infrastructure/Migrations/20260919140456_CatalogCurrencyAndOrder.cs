using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Habbo.Domain.Catalog;

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogCurrencyAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_page_offers_catalog_offers_OffersId",
                table: "catalog_page_offers");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_page_offers_catalog_pages_PagesId",
                table: "catalog_page_offers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_catalog_page_offers",
                table: "catalog_page_offers");

            migrationBuilder.DropIndex(
                name: "IX_catalog_page_offers_PagesId",
                table: "catalog_page_offers");

            // Scaffolded as a rename of PagesId to OrderIndex plus a new PageId. That is
            // positional matching, not semantic: it would move every page id into the order
            // column and leave all 3,722 links pointing at page 0. PagesId is the page id.
            migrationBuilder.RenameColumn(
                name: "PagesId",
                table: "catalog_page_offers",
                newName: "PageId");

            migrationBuilder.RenameColumn(
                name: "OffersId",
                table: "catalog_page_offers",
                newName: "OfferId");

            migrationBuilder.RenameIndex(
                name: "IX_catalog_page_offers_OffersId",
                table: "catalog_page_offers",
                newName: "IX_catalog_page_offers_OfferId");

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "catalog_page_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing links predate the column, so their real order is lost. Numbering them
            // by offer id per page preserves exactly the order they were served in until now,
            // rather than collapsing every page onto 0.
            migrationBuilder.Sql("""
                UPDATE catalog_page_offers l
                SET "OrderIndex" = ranked.position - 1
                FROM (
                    SELECT "PageId", "OfferId",
                           row_number() OVER (PARTITION BY "PageId" ORDER BY "OfferId") AS position
                    FROM catalog_page_offers
                ) AS ranked
                WHERE l."PageId" = ranked."PageId" AND l."OfferId" = ranked."OfferId";
                """);

            migrationBuilder.AddColumn<int>(
                name: "ActivityPointType",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Giftable",
                table: "catalog_offers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPet",
                table: "catalog_offers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRentable",
                table: "catalog_offers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviewImage",
                table: "catalog_offers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PriceInActivityPoints",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriceInSilver",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_catalog_page_offers",
                table: "catalog_page_offers",
                columns: new[] { "PageId", "OfferId" });

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_page_offers_catalog_offers_OfferId",
                table: "catalog_page_offers",
                column: "OfferId",
                principalTable: "catalog_offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_page_offers_catalog_pages_PageId",
                table: "catalog_page_offers",
                column: "PageId",
                principalTable: "catalog_pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_page_offers_catalog_offers_OfferId",
                table: "catalog_page_offers");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_page_offers_catalog_pages_PageId",
                table: "catalog_page_offers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_catalog_page_offers",
                table: "catalog_page_offers");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "catalog_page_offers");

            migrationBuilder.DropColumn(
                name: "ActivityPointType",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "Giftable",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "IsPet",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "IsRentable",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "PreviewImage",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "PriceInActivityPoints",
                table: "catalog_offers");

            migrationBuilder.DropColumn(
                name: "PriceInSilver",
                table: "catalog_offers");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "catalog_page_offers",
                newName: "PagesId");

            migrationBuilder.RenameColumn(
                name: "OfferId",
                table: "catalog_page_offers",
                newName: "OffersId");

            migrationBuilder.RenameIndex(
                name: "IX_catalog_page_offers_OfferId",
                table: "catalog_page_offers",
                newName: "IX_catalog_page_offers_OffersId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_catalog_page_offers",
                table: "catalog_page_offers",
                columns: new[] { "OffersId", "PagesId" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_page_offers_PagesId",
                table: "catalog_page_offers",
                column: "PagesId");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_page_offers_catalog_offers_OffersId",
                table: "catalog_page_offers",
                column: "OffersId",
                principalTable: "catalog_offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_page_offers_catalog_pages_PagesId",
                table: "catalog_page_offers",
                column: "PagesId",
                principalTable: "catalog_pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
