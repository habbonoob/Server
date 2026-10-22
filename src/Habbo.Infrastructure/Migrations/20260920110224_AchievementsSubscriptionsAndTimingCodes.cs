using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AchievementsSubscriptionsAndTimingCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BadgeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayMethod = table.Column<int>(type: "integer", nullable: false),
                    RewardPointType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "builders_club_limits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FurniLimit = table.Column<int>(type: "integer", nullable: false),
                    MaxFurniLimit = table.Column<int>(type: "integer", nullable: false),
                    Grace = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_builders_club_limits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodsSubscribedAhead = table.Column<int>(type: "integer", nullable: false),
                    MemberPeriods = table.Column<int>(type: "integer", nullable: false),
                    PastClubDays = table.Column<int>(type: "integer", nullable: false),
                    PastVipDays = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timing_schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SchedulingKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timing_schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "achievement_levels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchievementId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    RequiredProgress = table.Column<int>(type: "integer", nullable: false),
                    RewardPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_levels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_achievement_levels_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => new { x.UserId, x.AchievementId });
                    table.ForeignKey(
                        name: "FK_user_achievements_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_achievements_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timing_schedule_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timing_schedule_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timing_schedule_entries_timing_schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "timing_schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_AchievementId_Level",
                table: "achievement_levels",
                columns: new[] { "AchievementId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_BadgeCode",
                table: "achievements",
                column: "BadgeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_builders_club_limits_ProductName",
                table: "builders_club_limits",
                column: "ProductName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_ProductName",
                table: "subscriptions",
                columns: new[] { "UserId", "ProductName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timing_schedule_entries_ScheduleId_StartsAt",
                table: "timing_schedule_entries",
                columns: new[] { "ScheduleId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_timing_schedules_SchedulingKey",
                table: "timing_schedules",
                column: "SchedulingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_AchievementId",
                table: "user_achievements",
                column: "AchievementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement_levels");

            migrationBuilder.DropTable(
                name: "builders_club_limits");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "timing_schedule_entries");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "timing_schedules");

            migrationBuilder.DropTable(
                name: "achievements");
        }
    }
}
