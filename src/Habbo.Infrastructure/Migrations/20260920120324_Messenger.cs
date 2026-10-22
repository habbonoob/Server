using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Messenger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blocked_users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocked_users", x => new { x.UserId, x.BlockedUserId });
                    table.ForeignKey(
                        name: "FK_blocked_users_users_BlockedUserId",
                        column: x => x.BlockedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blocked_users_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "friend_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_friend_categories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "friend_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_friend_requests_users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friend_requests_users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ignored_users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IgnoredUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ignored_users", x => new { x.UserId, x.IgnoredUserId });
                    table.ForeignKey(
                        name: "FK_ignored_users_users_IgnoredUserId",
                        column: x => x.IgnoredUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ignored_users_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "friendships",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    Relationship = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friendships", x => new { x.UserId, x.FriendId });
                    table.ForeignKey(
                        name: "FK_friendships_friend_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "friend_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_friendships_users_FriendId",
                        column: x => x.FriendId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friendships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blocked_users_BlockedUserId",
                table: "blocked_users",
                column: "BlockedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_friend_categories_UserId",
                table: "friend_categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_RequesterId_TargetId",
                table: "friend_requests",
                columns: new[] { "RequesterId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_TargetId",
                table: "friend_requests",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_friendships_CategoryId",
                table: "friendships",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_friendships_FriendId",
                table: "friendships",
                column: "FriendId");

            migrationBuilder.CreateIndex(
                name: "IX_ignored_users_IgnoredUserId",
                table: "ignored_users",
                column: "IgnoredUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_users");

            migrationBuilder.DropTable(
                name: "friend_requests");

            migrationBuilder.DropTable(
                name: "friendships");

            migrationBuilder.DropTable(
                name: "ignored_users");

            migrationBuilder.DropTable(
                name: "friend_categories");
        }
    }
}
