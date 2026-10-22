using System.Collections.Generic;
using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    PageName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Localization = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Icon = table.Column<int>(type: "integer", nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    LayoutCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Images = table.Column<List<string>>(type: "text[]", nullable: false),
                    Texts = table.Column<List<string>>(type: "text[]", nullable: false),
                    MinRank = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_pages_catalog_pages_ParentId",
                        column: x => x.ParentId,
                        principalTable: "catalog_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    LocalizationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PriceInCredits = table.Column<int>(type: "integer", nullable: false),
                    ClubLevel = table.Column<int>(type: "integer", nullable: false),
                    MaxAmount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_offers_catalog_pages_PageId",
                        column: x => x.PageId,
                        principalTable: "catalog_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    DefinitionId = table.Column<int>(type: "integer", nullable: true),
                    ExtraParam = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_products_catalog_offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "catalog_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_products_item_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "item_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offers_PageId",
                table: "catalog_offers",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_pages_ParentId",
                table: "catalog_pages",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_products_DefinitionId",
                table: "catalog_products",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_products_OfferId",
                table: "catalog_products",
                column: "OfferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_products");

            migrationBuilder.DropTable(
                name: "catalog_offers");

            migrationBuilder.DropTable(
                name: "catalog_pages");
        }
    }
}
