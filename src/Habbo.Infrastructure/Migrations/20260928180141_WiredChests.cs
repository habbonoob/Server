using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WiredChests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChestDrawOrder",
                table: "user_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChestId",
                table: "user_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StoredAt",
                table: "user_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StoredInTransactionId",
                table: "user_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chest_coins",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Coins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chest_coins", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_chest_coins_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chest_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Info = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChestIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    DepositedFurni = table.Column<int>(type: "integer", nullable: false),
                    WithdrawnFurni = table.Column<int>(type: "integer", nullable: false),
                    DepositedCoins = table.Column<int>(type: "integer", nullable: false),
                    WithdrawnCoins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chest_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "deferred_notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ParameterKeys = table.Column<List<string>>(type: "text[]", nullable: false),
                    ParameterValues = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deferred_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deferred_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chest_transaction_items",
                columns: table => new
                {
                    TransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Deposited = table.Column<bool>(type: "boolean", nullable: false),
                    IsWallItem = table.Column<bool>(type: "boolean", nullable: false),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    PosterId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chest_transaction_items", x => new { x.TransactionId, x.Deposited, x.IsWallItem, x.TypeId, x.PosterId });
                    table.ForeignKey(
                        name: "FK_chest_transaction_items_chest_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "chest_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_items_ChestId",
                table: "user_items",
                column: "ChestId");

            migrationBuilder.CreateIndex(
                name: "IX_chest_transactions_RoomId",
                table: "chest_transactions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_deferred_notifications_UserId",
                table: "deferred_notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_items_user_items_ChestId",
                table: "user_items",
                column: "ChestId",
                principalTable: "user_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_items_user_items_ChestId",
                table: "user_items");

            migrationBuilder.DropTable(
                name: "chest_coins");

            migrationBuilder.DropTable(
                name: "chest_transaction_items");

            migrationBuilder.DropTable(
                name: "deferred_notifications");

            migrationBuilder.DropTable(
                name: "chest_transactions");

            migrationBuilder.DropIndex(
                name: "IX_user_items_ChestId",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "ChestDrawOrder",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "ChestId",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "StoredAt",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "StoredInTransactionId",
                table: "user_items");
        }
    }
}
