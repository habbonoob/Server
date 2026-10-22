using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <summary>
    /// The new navigator laid out as Habbo's (seed/navigator.json): the seed only adds rows by their codes, so the
    /// old default blocks it replaces, and the tab quick links Habbo does not have (it sends each tab with no saved
    /// searches; a player's own come in their own message), are taken out here. The seed adds the new rows on start,
    /// a new tab after the existing ones: "My World" moves along so the events tab lands before it, as on Habbo
    /// (Public, All Rooms, Events, My World).
    /// </summary>
    public partial class NavigatorAsHabbo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM navigator_blocks
                WHERE ("RequestCode", "BlockCode") IN (
                    ('official_view', 'official_view'),
                    ('hotel_view', 'popular'),
                    ('popular', 'popular'),
                    ('myworld_view', 'my_rooms'),
                    ('my_rooms', 'my_rooms'),
                    ('favorites', 'favorites'));
                DELETE FROM navigator_quick_links;
                UPDATE navigator_tabs SET "SortOrder" = 4 WHERE "SearchCode" = 'myworld_view';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // the old rows are the seed's to bring back, from an older navigator.json
        }
    }
}
