using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmailChangeCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NameColor",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ProfileHidden",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RespectReplenishedOn",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RespectReplenishes",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StarGems",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TradeLocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StreakStartedAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "badge_requests",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BadgeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badge_requests", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "chat_styles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    AssetId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SystemStyle = table.Column<bool>(type: "boolean", nullable: false),
                    Purchasable = table.Column<bool>(type: "boolean", nullable: false),
                    HcOnly = table.Column<bool>(type: "boolean", nullable: false),
                    StaffOverrideable = table.Column<bool>(type: "boolean", nullable: false),
                    AmbassadorOnly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_styles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "club_paydays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreditsSpent = table.Column<int>(type: "integer", nullable: false),
                    SpendReward = table.Column<int>(type: "integer", nullable: false),
                    StreakBonus = table.Column<int>(type: "integer", nullable: false),
                    WasMember = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_club_paydays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_club_paydays_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "currency_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_currency_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "currency_types",
                columns: table => new
                {
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InitialAmount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_types", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "star_gem_gifts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    GiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    GivenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_star_gem_gifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_star_gem_gifts_users_GiverId",
                        column: x => x.GiverId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_star_gem_gifts_users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_badges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_badges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_badges_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_chat_styles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StyleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_chat_styles", x => new { x.UserId, x.StyleId });
                    table.ForeignKey(
                        name: "FK_user_chat_styles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_currencies",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_currencies", x => new { x.UserId, x.Type });
                    table.ForeignKey(
                        name: "FK_user_currencies_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_effects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectId = table.Column<int>(type: "integer", nullable: false),
                    SubType = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Inactive = table.Column<int>(type: "integer", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsPermanent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_effects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_effects_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_figure_sets",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetId = table.Column<int>(type: "integer", nullable: false),
                    FurniClassName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_figure_sets", x => new { x.UserId, x.SetId });
                    table.ForeignKey(
                        name: "FK_user_figure_sets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_outfits",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Figure = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_outfits", x => new { x.UserId, x.Slot });
                    table.ForeignKey(
                        name: "FK_user_outfits_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_club_paydays_UserId_PaidAt",
                table: "club_paydays",
                columns: new[] { "UserId", "PaidAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currency_transactions_UserId_Currency_CreatedAt",
                table: "currency_transactions",
                columns: new[] { "UserId", "Currency", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_star_gem_gifts_GiverId_GivenAt",
                table: "star_gem_gifts",
                columns: new[] { "GiverId", "GivenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_star_gem_gifts_ReceiverId",
                table: "star_gem_gifts",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_UserId_Code",
                table: "user_badges",
                columns: new[] { "UserId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_effects_UserId_EffectId",
                table: "user_effects",
                columns: new[] { "UserId", "EffectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "badge_requests");

            migrationBuilder.DropTable(
                name: "chat_styles");

            migrationBuilder.DropTable(
                name: "club_paydays");

            migrationBuilder.DropTable(
                name: "currency_transactions");

            migrationBuilder.DropTable(
                name: "currency_types");

            migrationBuilder.DropTable(
                name: "star_gem_gifts");

            migrationBuilder.DropTable(
                name: "user_badges");

            migrationBuilder.DropTable(
                name: "user_chat_styles");

            migrationBuilder.DropTable(
                name: "user_currencies");

            migrationBuilder.DropTable(
                name: "user_effects");

            migrationBuilder.DropTable(
                name: "user_figure_sets");

            migrationBuilder.DropTable(
                name: "user_outfits");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailChangeCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NameColor",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ProfileHidden",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RespectReplenishedOn",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RespectReplenishes",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StarGems",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TradeLocked",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StreakStartedAt",
                table: "subscriptions");
        }
    }
}
