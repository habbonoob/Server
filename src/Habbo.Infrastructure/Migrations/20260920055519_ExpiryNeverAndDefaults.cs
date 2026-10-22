using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpiryNeverAndDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Habbo sends -1 for furniture that does not expire; 0 reads as a rental that has
            // just run out. Nothing rents yet, so every stored 0 means "never".
            migrationBuilder.Sql("UPDATE user_items SET \"ExpiryTime\" = -1 WHERE \"ExpiryTime\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE user_items SET \"ExpiryTime\" = 0 WHERE \"ExpiryTime\" = -1;");
        }
    }
}
