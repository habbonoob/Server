using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Marketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InMarketplace",
                table: "user_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "marketplace_offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    Revenue = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SoldAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Redeemed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketplace_offers_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_marketplace_offers_users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tokens = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_tokens", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_marketplace_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_ItemId",
                table: "marketplace_offers",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_SellerId",
                table: "marketplace_offers",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_SoldAt_ExpiresAt",
                table: "marketplace_offers",
                columns: new[] { "SoldAt", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketplace_offers");

            migrationBuilder.DropTable(
                name: "marketplace_tokens");

            migrationBuilder.DropColumn(
                name: "InMarketplace",
                table: "user_items");
        }
    }
}
