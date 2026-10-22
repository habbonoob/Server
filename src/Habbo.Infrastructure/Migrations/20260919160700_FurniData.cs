using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FurniData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataType",
                table: "user_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UniqueSerialNumber",
                table: "user_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniqueSeriesSize",
                table: "user_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "furni_crackables",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Hits = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_crackables", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_crackables_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_high_scores",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ScoreType = table.Column<int>(type: "integer", nullable: false),
                    ClearType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_high_scores", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_high_scores_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_int_entries",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_int_entries", x => new { x.ItemId, x.Position });
                    table.ForeignKey(
                        name: "FK_furni_int_entries_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_map_entries",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_map_entries", x => new { x.ItemId, x.Key });
                    table.ForeignKey(
                        name: "FK_furni_map_entries_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_states",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_states", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_states_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_string_entries",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_string_entries", x => new { x.ItemId, x.Position });
                    table.ForeignKey(
                        name: "FK_furni_string_entries_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_vote_results",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_vote_results", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_furni_vote_results_user_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "user_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_high_score_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_high_score_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_furni_high_score_entries_furni_high_scores_ItemId",
                        column: x => x.ItemId,
                        principalTable: "furni_high_scores",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "furni_high_score_users",
                columns: table => new
                {
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_furni_high_score_users", x => new { x.EntryId, x.Position });
                    table.ForeignKey(
                        name: "FK_furni_high_score_users_furni_high_score_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "furni_high_score_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_furni_high_score_entries_ItemId_Position",
                table: "furni_high_score_entries",
                columns: new[] { "ItemId", "Position" },
                unique: true);

            // Carried over before the old column goes. Every existing item used the single-string
            // layout (the only one we ever wrote), so a non-empty value becomes its state row and
            // DataType keeps its default of 0. Dropping first, as scaffolded, would lose them.
            migrationBuilder.Sql("""
                INSERT INTO furni_states ("ItemId", "State")
                SELECT "Id", "ExtraData" FROM user_items WHERE "ExtraData" <> '';
                """);

            migrationBuilder.DropColumn(
                name: "ExtraData",
                table: "user_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "furni_crackables");

            migrationBuilder.DropTable(
                name: "furni_high_score_users");

            migrationBuilder.DropTable(
                name: "furni_int_entries");

            migrationBuilder.DropTable(
                name: "furni_map_entries");

            migrationBuilder.DropTable(
                name: "furni_states");

            migrationBuilder.DropTable(
                name: "furni_string_entries");

            migrationBuilder.DropTable(
                name: "furni_vote_results");

            migrationBuilder.DropTable(
                name: "furni_high_score_entries");

            migrationBuilder.DropTable(
                name: "furni_high_scores");

            migrationBuilder.DropColumn(
                name: "DataType",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "UniqueSerialNumber",
                table: "user_items");

            migrationBuilder.DropColumn(
                name: "UniqueSeriesSize",
                table: "user_items");

            migrationBuilder.AddColumn<string>(
                name: "ExtraData",
                table: "user_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
