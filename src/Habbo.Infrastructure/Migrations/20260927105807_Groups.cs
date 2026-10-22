using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Groups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavouriteGroupId",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "group_badge_parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MaskFileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_badge_parts", x => new { x.Kind, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "group_colors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Hex = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_colors", x => new { x.Kind, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BadgeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryColorId = table.Column<int>(type: "integer", nullable: false),
                    SecondaryColorId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    MembersCanDecorate = table.Column<bool>(type: "boolean", nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    Url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasForum = table.Column<bool>(type: "boolean", nullable: false),
                    ForumReadPermission = table.Column<int>(type: "integer", nullable: false),
                    ForumPostMessagePermission = table.Column<int>(type: "integer", nullable: false),
                    ForumPostThreadPermission = table.Column<int>(type: "integer", nullable: false),
                    ForumModeratePermission = table.Column<int>(type: "integer", nullable: false),
                    ForumDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    ForumViews = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_groups_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_groups_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forum_read_markers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    LastReadMessageIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_read_markers", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_forum_read_markers_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_forum_read_markers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forum_threads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Sticky = table.Column<bool>(type: "boolean", nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminActedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_threads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_forum_threads_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_forum_threads_users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_forum_threads_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_badge_layers",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    Layer = table.Column<int>(type: "integer", nullable: false),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    ColorId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_badge_layers", x => new { x.GroupId, x.Layer });
                    table.ForeignKey(
                        name: "FK_group_badge_layers_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_blocks",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_blocks", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_group_blocks_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_blocks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_members", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_group_members_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_membership_requests",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_membership_requests", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_group_membership_requests_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_membership_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forum_messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ThreadId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    State = table.Column<byte>(type: "smallint", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminActedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_forum_messages_forum_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "forum_threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_forum_messages_users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_forum_messages_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_FavouriteGroupId",
                table: "users",
                column: "FavouriteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_forum_messages_AdminId",
                table: "forum_messages",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_forum_messages_AuthorId",
                table: "forum_messages",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_forum_messages_GroupId_Id",
                table: "forum_messages",
                columns: new[] { "GroupId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_forum_messages_ThreadId_Id",
                table: "forum_messages",
                columns: new[] { "ThreadId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_forum_read_markers_GroupId",
                table: "forum_read_markers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_forum_threads_AdminId",
                table: "forum_threads",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_forum_threads_AuthorId",
                table: "forum_threads",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_forum_threads_GroupId",
                table: "forum_threads",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_blocks_UserId",
                table: "group_blocks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_UserId",
                table: "group_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_group_membership_requests_UserId",
                table: "group_membership_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_OwnerId",
                table: "groups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_RoomId",
                table: "groups",
                column: "RoomId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_groups_FavouriteGroupId",
                table: "users",
                column: "FavouriteGroupId",
                principalTable: "groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_groups_FavouriteGroupId",
                table: "users");

            migrationBuilder.DropTable(
                name: "forum_messages");

            migrationBuilder.DropTable(
                name: "forum_read_markers");

            migrationBuilder.DropTable(
                name: "group_badge_layers");

            migrationBuilder.DropTable(
                name: "group_badge_parts");

            migrationBuilder.DropTable(
                name: "group_blocks");

            migrationBuilder.DropTable(
                name: "group_colors");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "group_membership_requests");

            migrationBuilder.DropTable(
                name: "forum_threads");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropIndex(
                name: "IX_users_FavouriteGroupId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FavouriteGroupId",
                table: "users");
        }
    }
}
