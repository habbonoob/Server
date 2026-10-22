using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceSalesOutliveItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_marketplace_offers_user_items_ItemId",
                table: "marketplace_offers");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_ItemId",
                table: "marketplace_offers");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "marketplace_offers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DefinitionId",
                table: "marketplace_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // every offer so far still has its item: its kind is the item's
            migrationBuilder.Sql("""
                UPDATE marketplace_offers AS o SET "DefinitionId" = i."DefinitionId"
                FROM user_items AS i WHERE i."Id" = o."ItemId";
                """);

            // an item on sale twice (two offers made at once) keeps its first offer; the item stays on sale
            migrationBuilder.Sql("""
                DELETE FROM marketplace_offers AS o
                WHERE o."SoldAt" IS NULL AND EXISTS (
                    SELECT 1 FROM marketplace_offers AS e
                    WHERE e."ItemId" = o."ItemId" AND e."SoldAt" IS NULL AND e."Id" < o."Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_DefinitionId",
                table: "marketplace_offers",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_ItemId",
                table: "marketplace_offers",
                column: "ItemId",
                unique: true,
                filter: "\"SoldAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_marketplace_offers_item_definitions_DefinitionId",
                table: "marketplace_offers",
                column: "DefinitionId",
                principalTable: "item_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_marketplace_offers_user_items_ItemId",
                table: "marketplace_offers",
                column: "ItemId",
                principalTable: "user_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_marketplace_offers_item_definitions_DefinitionId",
                table: "marketplace_offers");

            migrationBuilder.DropForeignKey(
                name: "FK_marketplace_offers_user_items_ItemId",
                table: "marketplace_offers");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_DefinitionId",
                table: "marketplace_offers");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_ItemId",
                table: "marketplace_offers");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "marketplace_offers");

            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "marketplace_offers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_ItemId",
                table: "marketplace_offers",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_marketplace_offers_user_items_ItemId",
                table: "marketplace_offers",
                column: "ItemId",
                principalTable: "user_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
