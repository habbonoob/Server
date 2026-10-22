using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InviteCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invite_codes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Note = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invite_codes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_invite_codes_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_invite_codes_users_UsedById",
                        column: x => x.UsedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invite_codes_CreatedAt",
                table: "invite_codes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_invite_codes_CreatedById",
                table: "invite_codes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_invite_codes_UsedById",
                table: "invite_codes",
                column: "UsedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invite_codes");
        }
    }
}
