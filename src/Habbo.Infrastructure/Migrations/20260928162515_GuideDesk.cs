using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GuideDesk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_review_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Verdict = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_review_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_review_records_users_ReportedId",
                        column: x => x.ReportedId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_review_records_users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_review_records_ReportedId",
                table: "chat_review_records",
                column: "ReportedId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_review_records_ReporterId_CreatedAt",
                table: "chat_review_records",
                columns: new[] { "ReporterId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_review_records");
        }
    }
}
