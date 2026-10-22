using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoomCompetitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_competition_entries",
                columns: table => new
                {
                    GoalId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_competition_entries", x => new { x.GoalId, x.RoomId });
                    table.ForeignKey(
                        name: "FK_room_competition_entries_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_competition_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_competition_rules_acceptances",
                columns: table => new
                {
                    GoalId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_competition_rules_acceptances", x => new { x.GoalId, x.UserId });
                    table.ForeignKey(
                        name: "FK_room_competition_rules_acceptances_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_competition_votes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    GoalId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_competition_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_competition_votes_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_competition_votes_users_VoterId",
                        column: x => x.VoterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_entries_GoalId_UserId",
                table: "room_competition_entries",
                columns: new[] { "GoalId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_entries_RoomId",
                table: "room_competition_entries",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_entries_UserId",
                table: "room_competition_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_rules_acceptances_UserId",
                table: "room_competition_rules_acceptances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_votes_GoalId_RoomId",
                table: "room_competition_votes",
                columns: new[] { "GoalId", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_votes_GoalId_VoterId_VotedAt",
                table: "room_competition_votes",
                columns: new[] { "GoalId", "VoterId", "VotedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_votes_RoomId",
                table: "room_competition_votes",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_room_competition_votes_VoterId",
                table: "room_competition_votes",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_competition_entries");

            migrationBuilder.DropTable(
                name: "room_competition_rules_acceptances");

            migrationBuilder.DropTable(
                name: "room_competition_votes");
        }
    }
}
