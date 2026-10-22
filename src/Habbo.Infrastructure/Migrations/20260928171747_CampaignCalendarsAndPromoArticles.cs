using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CampaignCalendarsAndPromoArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaign_calendars",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_calendars", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "promo_articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    ButtonText = table.Column<string>(type: "text", nullable: false),
                    LinkType = table.Column<int>(type: "integer", nullable: false),
                    LinkContent = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campaign_calendar_doors",
                columns: table => new
                {
                    CampaignName = table.Column<string>(type: "character varying(64)", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    FurniClassName = table.Column<string>(type: "text", nullable: false),
                    ProductCode = table.Column<string>(type: "text", nullable: false),
                    CustomImage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_calendar_doors", x => new { x.CampaignName, x.Day });
                    table.ForeignKey(
                        name: "FK_campaign_calendar_doors_campaign_calendars_CampaignName",
                        column: x => x.CampaignName,
                        principalTable: "campaign_calendars",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_calendar_openings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignName = table.Column<string>(type: "character varying(64)", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_calendar_openings", x => new { x.UserId, x.CampaignName, x.Day });
                    table.ForeignKey(
                        name: "FK_campaign_calendar_openings_campaign_calendars_CampaignName",
                        column: x => x.CampaignName,
                        principalTable: "campaign_calendars",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_campaign_calendar_openings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_campaign_calendar_openings_CampaignName",
                table: "campaign_calendar_openings",
                column: "CampaignName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_calendar_doors");

            migrationBuilder.DropTable(
                name: "campaign_calendar_openings");

            migrationBuilder.DropTable(
                name: "promo_articles");

            migrationBuilder.DropTable(
                name: "campaign_calendars");
        }
    }
}
