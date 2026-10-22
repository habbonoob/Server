using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogIdentityIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            // Both tables hold ids chosen elsewhere — Habbo's for a dumped catalog, the seed
            // file's for ours — so their sequences still sit at 1. Creating a page or an offer
            // in the hotel itself would be handed id 1 and collide on the spot.
            // GREATEST guards the floor, because a hidden page's derived id is negative.
            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('catalog_pages', 'Id'),
                    GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM catalog_pages), 1));
                """);

            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('catalog_offers', 'Id'),
                    GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM catalog_offers), 1));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "catalog_offers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
