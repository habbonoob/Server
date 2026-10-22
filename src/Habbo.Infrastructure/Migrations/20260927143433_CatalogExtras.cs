using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CatalogNewAdditionsSeenAt",
                table: "user_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "catalog_pages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpiryImage",
                table: "catalog_pages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvailableFrom",
                table: "catalog_offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bonus_rare_progress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditsSpent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bonus_rare_progress", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_bonus_rare_progress_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "targeted_offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OfferId = table.Column<int>(type: "integer", nullable: false),
                    PriceInCredits = table.Column<int>(type: "integer", nullable: false),
                    PriceInActivityPoints = table.Column<int>(type: "integer", nullable: false),
                    ActivityPointType = table.Column<int>(type: "integer", nullable: false),
                    PurchaseLimit = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IconImageUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SubProductCodes = table.Column<List<string>>(type: "text[]", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_targeted_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_targeted_offers_catalog_offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "catalog_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Credits = table.Column<int>(type: "integer", nullable: false),
                    ActivityPoints = table.Column<int>(type: "integer", nullable: false),
                    ActivityPointType = table.Column<int>(type: "integer", nullable: false),
                    OfferId = table.Column<int>(type: "integer", nullable: true),
                    MaxUses = table.Column<int>(type: "integer", nullable: false),
                    Uses = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchers", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "targeted_offer_states",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetedOfferId = table.Column<int>(type: "integer", nullable: false),
                    TrackingState = table.Column<int>(type: "integer", nullable: false),
                    Purchased = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_targeted_offer_states", x => new { x.UserId, x.TargetedOfferId });
                    table.ForeignKey(
                        name: "FK_targeted_offer_states_targeted_offers_TargetedOfferId",
                        column: x => x.TargetedOfferId,
                        principalTable: "targeted_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_targeted_offer_states_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "voucher_redemptions",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_redemptions", x => new { x.Code, x.UserId });
                    table.ForeignKey(
                        name: "FK_voucher_redemptions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_voucher_redemptions_vouchers_Code",
                        column: x => x.Code,
                        principalTable: "vouchers",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_targeted_offer_states_TargetedOfferId",
                table: "targeted_offer_states",
                column: "TargetedOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_targeted_offers_OfferId",
                table: "targeted_offers",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_redemptions_UserId",
                table: "voucher_redemptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bonus_rare_progress");

            migrationBuilder.DropTable(
                name: "targeted_offer_states");

            migrationBuilder.DropTable(
                name: "voucher_redemptions");

            migrationBuilder.DropTable(
                name: "targeted_offers");

            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropColumn(
                name: "CatalogNewAdditionsSeenAt",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "catalog_pages");

            migrationBuilder.DropColumn(
                name: "ExpiryImage",
                table: "catalog_pages");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                table: "catalog_offers");
        }
    }
}
