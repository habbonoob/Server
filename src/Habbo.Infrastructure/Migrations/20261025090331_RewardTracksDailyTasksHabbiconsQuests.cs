using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RewardTracksDailyTasksHabbiconsQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HabbiconId",
                table: "console_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "daily_tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Repeats = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_tasks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reward_track_progress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Premium = table.Column<bool>(type: "boolean", nullable: false),
                    Tasks = table.Column<string>(type: "jsonb", nullable: false),
                    Claimed = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_track_progress", x => new { x.UserId, x.TrackId });
                    table.ForeignKey(
                        name: "FK_reward_track_progress_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_habbicons",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HabbiconId = table.Column<int>(type: "integer", nullable: false),
                    Favourite = table.Column<bool>(type: "boolean", nullable: false),
                    AcquiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_habbicons", x => new { x.UserId, x.HabbiconId });
                    table.ForeignKey(
                        name: "FK_user_habbicons_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_seasonal_quests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<int>(type: "integer", nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_seasonal_quests", x => new { x.UserId, x.QuestId });
                    table.ForeignKey(
                        name: "FK_user_seasonal_quests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_tasks_UserId_Day",
                table: "daily_tasks",
                columns: new[] { "UserId", "Day" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_tasks");

            migrationBuilder.DropTable(
                name: "reward_track_progress");

            migrationBuilder.DropTable(
                name: "user_habbicons");

            migrationBuilder.DropTable(
                name: "user_seasonal_quests");

            migrationBuilder.DropColumn(
                name: "HabbiconId",
                table: "console_messages");
        }
    }
}
