using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_claims",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Furni = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_claim_records",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<string>(type: "character varying(64)", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_claim_records", x => new { x.UserId, x.ClaimId });
                    table.ForeignKey(
                        name: "FK_product_claim_records_product_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "product_claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_claim_records_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_claim_records_ClaimId",
                table: "product_claim_records",
                column: "ClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_claim_records");

            migrationBuilder.DropTable(
                name: "product_claims");
        }
    }
}
