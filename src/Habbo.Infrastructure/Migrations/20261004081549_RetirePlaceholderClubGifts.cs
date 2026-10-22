using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetirePlaceholderClubGifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The club gifts page listed ten placeholder gifts (the classic HC furniture, ids 3000101-3000110) that
            // seed/club.json once named; it now names the hotel's own gifts, so the placeholders leave the page. The
            // offers themselves stay, for any gift already claimed.
            migrationBuilder.Sql("DELETE FROM catalog_page_offers WHERE \"OfferId\" BETWEEN 3000101 AND 3000110;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
