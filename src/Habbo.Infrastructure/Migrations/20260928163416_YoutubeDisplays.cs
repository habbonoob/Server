using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class YoutubeDisplays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "youtube_playlists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "youtube_display_states",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    PlaylistId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Paused = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_display_states", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_youtube_display_states_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_youtube_display_states_youtube_playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "youtube_playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "youtube_videos",
                columns: table => new
                {
                    PlaylistId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    VideoId = table.Column<string>(type: "text", nullable: false),
                    StartAtSeconds = table.Column<int>(type: "integer", nullable: false),
                    EndAtSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_videos", x => new { x.PlaylistId, x.Position });
                    table.ForeignKey(
                        name: "FK_youtube_videos_youtube_playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "youtube_playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_youtube_display_states_PlaylistId",
                table: "youtube_display_states",
                column: "PlaylistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "youtube_display_states");

            migrationBuilder.DropTable(
                name: "youtube_videos");

            migrationBuilder.DropTable(
                name: "youtube_playlists");
        }
    }
}
