using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AchievementResolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "furni_achievement_resolutions",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<int>(type: "integer", nullable: false),
                    TargetLevel = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_achievement_resolutions", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_achievement_resolutions_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_furni_achievement_resolutions_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_furni_achievement_resolutions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_furni_achievement_resolutions_AchievementId",
                table: "furni_achievement_resolutions",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_furni_achievement_resolutions_UserId_AchievementId",
                table: "furni_achievement_resolutions",
                columns: new[] { "UserId", "AchievementId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_achievement_resolutions");
        }
    }
}
