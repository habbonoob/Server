using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NftMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNft",
                table: "marketplace_offers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNft",
                table: "marketplace_offers");
        }
    }
}
