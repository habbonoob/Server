using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_models",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Heightmap = table.Column<string>(type: "text", nullable: false),
                    DoorX = table.Column<int>(type: "integer", nullable: false),
                    DoorY = table.Column<int>(type: "integer", nullable: false),
                    DoorDirection = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AccountId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Figure = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Sex = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Motto = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Credits = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "web_gamedata",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_gamedata", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rooms_room_models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "room_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rooms_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sso_tickets",
                columns: table => new
                {
                    Ticket = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sso_tickets", x => x.Ticket);
                    table.ForeignKey(
                        name: "FK_sso_tickets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_models_Name",
                table: "room_models",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_ModelId",
                table: "rooms",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_OwnerId",
                table: "rooms",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_sso_tickets_ExpiresAt_ConsumedAt",
                table: "sso_tickets",
                columns: new[] { "ExpiresAt", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sso_tickets_UserId",
                table: "sso_tickets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_AccountId",
                table: "users",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "sso_tickets");

            migrationBuilder.DropTable(
                name: "web_gamedata");

            migrationBuilder.DropTable(
                name: "room_models");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
