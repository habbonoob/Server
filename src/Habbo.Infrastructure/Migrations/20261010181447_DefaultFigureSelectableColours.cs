using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DefaultFigureSelectableColours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // the old default wore colour 62 on the shirt and shoes, which the avatar editor never offers: the figure
            // check refused any look kept from it, so these accounts could not change clothes
            migrationBuilder.Sql(
                """
                UPDATE users SET "Figure" = 'hr-115-42.hd-190-1.ch-215-1408.lg-285-91.sh-290-1408'
                WHERE "Figure" = 'hr-115-42.hd-190-1.ch-215-62.lg-285-91.sh-290-62';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
