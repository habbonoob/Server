using System;
using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UiVolume = table.Column<int>(type: "integer", nullable: false),
                    FurniVolume = table.Column<int>(type: "integer", nullable: false),
                    TraxVolume = table.Column<int>(type: "integer", nullable: false),
                    FreeFlowChatDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    RoomInvitesIgnored = table.Column<bool>(type: "boolean", nullable: false),
                    RoomCameraFollowDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    UiFlags = table.Column<int>(type: "integer", nullable: false),
                    PreferedChatStyle = table.Column<int>(type: "integer", nullable: false),
                    WiredMenuButton = table.Column<bool>(type: "boolean", nullable: false),
                    WiredInspectButton = table.Column<bool>(type: "boolean", nullable: false),
                    PlayTestMode = table.Column<bool>(type: "boolean", nullable: false),
                    WiredWhisperDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShowAllNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    WiredUiStyle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_preferences");
        }
    }
}
