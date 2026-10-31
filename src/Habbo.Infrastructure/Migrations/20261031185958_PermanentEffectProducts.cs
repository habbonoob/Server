using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <summary>The effects on Habbo's "Effects" page (ny2015_permeffects) are the buyer's for good: a catalog seeded before
    /// they were told apart sold them with a day's duration, as the temporary ones are. No duration marks them permanent.</summary>
    public partial class PermanentEffectProducts : Migration
    {
        private const string PermanentEffectOffers = """
            "Type" = 2 AND "OfferId" IN (
                SELECT po."OfferId" FROM catalog_page_offers po
                JOIN catalog_pages pg ON pg."Id" = po."PageId"
                WHERE pg."PageName" = 'ny2015_permeffects')
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""UPDATE catalog_products SET "DurationSeconds" = 0 WHERE {PermanentEffectOffers};""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""UPDATE catalog_products SET "DurationSeconds" = 86400 WHERE {PermanentEffectOffers};""");
        }
    }
}
