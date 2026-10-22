using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NavigatorDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThumbnailBackgroundImageId",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThumbnailForegroundImageId",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThumbnailFrontImageId",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "competition_room_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    GoalId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Votes = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competition_room_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competition_room_entries_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_competition_room_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forward_rooms",
                columns: table => new
                {
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forward_rooms", x => x.Type);
                    table.ForeignKey(
                        name: "FK_forward_rooms_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "official_room_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    FolderId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PopupCaption = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PopupDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ShowDetails = table.Column<bool>(type: "boolean", nullable: false),
                    PicText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PicRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    Open = table.Column<bool>(type: "boolean", nullable: false),
                    IsAd = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_official_room_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_official_room_entries_official_room_entries_FolderId",
                        column: x => x.FolderId,
                        principalTable: "official_room_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_official_room_entries_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_events_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_events_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_networks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RoomIds = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_networks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "room_votes",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_votes", x => new { x.RoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_room_votes_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_votes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_ad_interactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Position = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_ad_interactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_ad_interactions_room_events_EventId",
                        column: x => x.EventId,
                        principalTable: "room_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_ad_interactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_competition_room_entries_GoalId_RoomId",
                table: "competition_room_entries",
                columns: new[] { "GoalId", "RoomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competition_room_entries_RoomId",
                table: "competition_room_entries",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_competition_room_entries_UserId",
                table: "competition_room_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_forward_rooms_RoomId",
                table: "forward_rooms",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_official_room_entries_FolderId",
                table: "official_room_entries",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_official_room_entries_RoomId",
                table: "official_room_entries",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_room_ad_interactions_EventId_Kind",
                table: "room_ad_interactions",
                columns: new[] { "EventId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_room_ad_interactions_UserId",
                table: "room_ad_interactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_events_ExpiresAt",
                table: "room_events",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_room_events_OwnerId",
                table: "room_events",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_room_events_RoomId_ExpiresAt",
                table: "room_events",
                columns: new[] { "RoomId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_room_votes_UserId",
                table: "room_votes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competition_room_entries");

            migrationBuilder.DropTable(
                name: "forward_rooms");

            migrationBuilder.DropTable(
                name: "official_room_entries");

            migrationBuilder.DropTable(
                name: "room_ad_interactions");

            migrationBuilder.DropTable(
                name: "room_networks");

            migrationBuilder.DropTable(
                name: "room_votes");

            migrationBuilder.DropTable(
                name: "room_events");

            migrationBuilder.DropColumn(
                name: "ThumbnailBackgroundImageId",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ThumbnailForegroundImageId",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "ThumbnailFrontImageId",
                table: "rooms");
        }
    }
}
