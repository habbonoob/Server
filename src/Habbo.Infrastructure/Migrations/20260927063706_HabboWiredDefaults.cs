using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HabboWiredDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WiredReadPermissionMask",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "WiredModifyPermissionMask",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "WallItemLimit",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 3872,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 500);

            migrationBuilder.AlterColumn<int>(
                name: "FloorItemLimit",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 3872,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2500);

            // rooms still on the old defaults take Habbo's
            migrationBuilder.Sql("UPDATE rooms SET \"FloorItemLimit\" = 3872 WHERE \"FloorItemLimit\" = 2500;");
            migrationBuilder.Sql("UPDATE rooms SET \"WallItemLimit\" = 3872 WHERE \"WallItemLimit\" = 500;");
            migrationBuilder.Sql("UPDATE rooms SET \"WiredModifyPermissionMask\" = 8 WHERE \"WiredModifyPermissionMask\" = 0;");
            migrationBuilder.Sql("UPDATE rooms SET \"WiredReadPermissionMask\" = 8 WHERE \"WiredReadPermissionMask\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WiredReadPermissionMask",
                table: "rooms",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "WiredModifyPermissionMask",
                table: "rooms",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "WallItemLimit",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 500,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3872);

            migrationBuilder.AlterColumn<int>(
                name: "FloorItemLimit",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 2500,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3872);
        }
    }
}
