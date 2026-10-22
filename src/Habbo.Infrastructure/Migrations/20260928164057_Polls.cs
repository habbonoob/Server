using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Polls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Headline = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    StartMessage = table.Column<string>(type: "text", nullable: false),
                    EndMessage = table.Column<string>(type: "text", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    NpsPoll = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_polls_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PollId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AnswerType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_questions_polls_PollId",
                        column: x => x.PollId,
                        principalTable: "polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_responses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    PollId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: true),
                    Answers = table.Column<List<string>>(type: "text[]", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_responses_polls_PollId",
                        column: x => x.PollId,
                        principalTable: "polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poll_responses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_choices",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    ChoiceType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_choices", x => new { x.QuestionId, x.Position });
                    table.ForeignKey(
                        name: "FK_poll_choices_poll_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "poll_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_poll_questions_PollId",
                table: "poll_questions",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_responses_PollId_UserId",
                table: "poll_responses",
                columns: new[] { "PollId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_poll_responses_UserId",
                table: "poll_responses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_polls_RoomId",
                table: "polls",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "poll_choices");

            migrationBuilder.DropTable(
                name: "poll_responses");

            migrationBuilder.DropTable(
                name: "poll_questions");

            migrationBuilder.DropTable(
                name: "polls");
        }
    }
}
