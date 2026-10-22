using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Quizzes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quizzes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AchievementName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quizzes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "quiz_questions",
                columns: table => new
                {
                    QuizCode = table.Column<string>(type: "character varying(64)", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswer = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_questions", x => new { x.QuizCode, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_quiz_questions_quizzes_QuizCode",
                        column: x => x.QuizCode,
                        principalTable: "quizzes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quiz_questions");

            migrationBuilder.DropTable(
                name: "quizzes");
        }
    }
}
