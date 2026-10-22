using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GroupFurniture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "user_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_items_GroupId",
                table: "user_items",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_items_groups_GroupId",
                table: "user_items",
                column: "GroupId",
                principalTable: "groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_items_groups_GroupId",
                table: "user_items");

            migrationBuilder.DropIndex(
                name: "IX_user_items_GroupId",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "user_items");
        }
    }
}
