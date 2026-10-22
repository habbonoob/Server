using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Moderation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MutedUntil",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TradeLockedUntil",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mod_tool_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WindowX = table.Column<int>(type: "integer", nullable: false),
                    WindowY = table.Column<int>(type: "integer", nullable: false),
                    WindowWidth = table.Column<int>(type: "integer", nullable: false),
                    WindowHeight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mod_tool_preferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_mod_tool_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moderation_issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    TopicId = table.Column<int>(type: "integer", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    ThreadId = table.Column<int>(type: "integer", nullable: true),
                    ForumMessageId = table.Column<int>(type: "integer", nullable: true),
                    PhotoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ReporterName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReporterEmail = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PickerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloseReason = table.Column<int>(type: "integer", nullable: true),
                    Sanctioned = table.Column<bool>(type: "boolean", nullable: false),
                    AppealState = table.Column<byte>(type: "smallint", nullable: false),
                    AppealedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AppealResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moderation_issues_users_PickerId",
                        column: x => x.PickerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_moderation_issues_users_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_moderation_issues_users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "moderation_presets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_presets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "room_chat_lines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_chat_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_chat_lines_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_chat_lines_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_visits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_visits_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_visits_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sanction_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    LengthHours = table.Column<int>(type: "integer", nullable: false),
                    ProbationDays = table.Column<int>(type: "integer", nullable: false),
                    Step = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanction_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sanctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SanctionTypeId = table.Column<int>(type: "integer", nullable: true),
                    TopicId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ModeratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssueId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvatarOnly = table.Column<bool>(type: "boolean", nullable: false),
                    MachineId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sanctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sanctions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moderation_issue_chat_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    IssueId = table.Column<int>(type: "integer", nullable: false),
                    UserAccountId = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_issue_chat_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moderation_issue_chat_lines_moderation_issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "moderation_issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_issue_chat_lines_IssueId",
                table: "moderation_issue_chat_lines",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_issues_PickerId",
                table: "moderation_issues",
                column: "PickerId");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_issues_ReportedUserId",
                table: "moderation_issues",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_issues_ReporterId_State",
                table: "moderation_issues",
                columns: new[] { "ReporterId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_issues_State",
                table: "moderation_issues",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_room_chat_lines_RoomId_Id",
                table: "room_chat_lines",
                columns: new[] { "RoomId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_room_chat_lines_UserId_Id",
                table: "room_chat_lines",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_room_visits_RoomId",
                table: "room_visits",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_room_visits_UserId_Id",
                table: "room_visits",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_MachineId",
                table: "sanctions",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_sanctions_UserId_Kind_ExpiresAt",
                table: "sanctions",
                columns: new[] { "UserId", "Kind", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mod_tool_preferences");

            migrationBuilder.DropTable(
                name: "moderation_issue_chat_lines");

            migrationBuilder.DropTable(
                name: "moderation_presets");

            migrationBuilder.DropTable(
                name: "room_chat_lines");

            migrationBuilder.DropTable(
                name: "room_visits");

            migrationBuilder.DropTable(
                name: "sanction_types");

            migrationBuilder.DropTable(
                name: "sanctions");

            migrationBuilder.DropTable(
                name: "moderation_issues");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MutedUntil",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TradeLockedUntil",
                table: "users");
        }
    }
}
