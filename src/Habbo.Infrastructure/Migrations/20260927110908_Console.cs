using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Console : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FollowingBlocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FriendRequestsBlocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "console_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    Text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_console_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_console_messages_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_console_messages_users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_console_messages_users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_console_messages_GroupId_Id",
                table: "console_messages",
                columns: new[] { "GroupId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_console_messages_Key",
                table: "console_messages",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_console_messages_RecipientId_Delivered",
                table: "console_messages",
                columns: new[] { "RecipientId", "Delivered" });

            migrationBuilder.CreateIndex(
                name: "IX_console_messages_SenderId_RecipientId_Id",
                table: "console_messages",
                columns: new[] { "SenderId", "RecipientId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "console_messages");

            migrationBuilder.DropColumn(
                name: "FollowingBlocked",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FriendRequestsBlocked",
                table: "users");
        }
    }
}
