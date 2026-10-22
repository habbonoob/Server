
#nullable disable
using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Habbo.Infrastructure.Migrations;

/// <summary>Adds the wire-visible columns the composers read: player profile fields, room
/// visualization/geometry settings, item attributes, and the furnidata category.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260918170000_WireVisibleFields")]
public partial class WireVisibleFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // users — UserObject / UserRights fields
        migrationBuilder.AddColumn<string>("RealName", "users", "character varying(64)", maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<bool>("DirectMail", "users", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("RespectTotal", "users", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("RespectLeft", "users", "integer", nullable: false, defaultValue: 3);
        migrationBuilder.AddColumn<int>("PetRespectLeft", "users", "integer", nullable: false, defaultValue: 3);
        migrationBuilder.AddColumn<bool>("StreamPublishingAllowed", "users", "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>("NameChangeAllowed", "users", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>("AccountSafetyLocked", "users", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("ClubLevel", "users", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>("IsAmbassador", "users", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("AchievementScore", "users", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>("IsModerator", "users", "boolean", nullable: false, defaultValue: false);

        // rooms — visualization + geometry the client reads on entry
        migrationBuilder.AddColumn<bool>("WallsHidden", "rooms", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("WallThicknessExponent", "rooms", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("FloorThicknessExponent", "rooms", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>("UseSmallScale", "rooms", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("FixedWallsHeight", "rooms", "integer", nullable: false, defaultValue: -1);
        migrationBuilder.AddColumn<int>("CameraInitX", "rooms", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("CameraInitY", "rooms", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<double>("CameraInitZ", "rooms", "double precision", nullable: false, defaultValue: 0.0);
        migrationBuilder.AddColumn<int>("OwnerControllerLevel", "rooms", "integer", nullable: false, defaultValue: 4);

        // user_items — inventory/trade attributes
        migrationBuilder.AddColumn<int>("Category", "user_items", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>("Recyclable", "user_items", "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>("Tradeable", "user_items", "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>("Groupable", "user_items", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>("Sellable", "user_items", "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<int>("SecondsToExpiration", "user_items", "integer", nullable: false, defaultValue: -1);
        migrationBuilder.AddColumn<bool>("HasRentPeriodStarted", "user_items", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("SlotId", "user_items", "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>("Extra", "user_items", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("ExpiryTime", "user_items", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>("UsagePolicy", "user_items", "integer", nullable: false, defaultValue: 0);

        // item_definitions — the furnidata category
        migrationBuilder.AddColumn<int>("Category", "item_definitions", "integer", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[]
                 {
                     "RealName", "DirectMail", "RespectTotal", "RespectLeft", "PetRespectLeft",
                     "StreamPublishingAllowed", "NameChangeAllowed", "AccountSafetyLocked",
                     "ClubLevel", "IsAmbassador", "AchievementScore", "IsModerator",
                 })
            migrationBuilder.DropColumn(column, "users");

        foreach (var column in new[]
                 {
                     "WallsHidden", "WallThicknessExponent", "FloorThicknessExponent", "UseSmallScale",
                     "FixedWallsHeight", "CameraInitX", "CameraInitY", "CameraInitZ", "OwnerControllerLevel",
                 })
            migrationBuilder.DropColumn(column, "rooms");

        foreach (var column in new[]
                 {
                     "Category", "Recyclable", "Tradeable", "Groupable", "Sellable", "SecondsToExpiration",
                     "HasRentPeriodStarted", "SlotId", "Extra", "ExpiryTime", "UsagePolicy",
                 })
            migrationBuilder.DropColumn(column, "user_items");

        migrationBuilder.DropColumn("Category", "item_definitions");
    }
}
