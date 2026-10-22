using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FurnidataImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "item_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FurniLine",
                table: "item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "item_definitions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "item_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            // Rows that predate this key used the furnidata class id as their primary key, so
            // that is exactly what their ClassId is. Backfilled before the unique index is
            // created, or every existing row would collide on the default of 0.
            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""UPDATE item_definitions SET "ClassId" = "Id";""");

            // Those same rows kept their old ids, which the new identity sequence knows nothing
            // about; without this the first generated id would be 1 and collide immediately.
            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('item_definitions', 'Id'),
                    GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM item_definitions), 1));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_item_definitions_ClassId_Type",
                table: "item_definitions",
                columns: new[] { "ClassId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_item_definitions_ClassId_Type",
                table: "item_definitions");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "item_definitions");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "item_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "FurniLine",
                table: "item_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "item_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "item_definitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "item_definitions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
