using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistWizardStateAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_draft",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    OwnerUserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    BaseRuleSystem = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    MixedModeEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverlaySourcesJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsFinalized = table.Column<bool>(type: "INTEGER", nullable: false),
                    StepsJson = table.Column<string>(type: "TEXT", nullable: false),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_draft", x => x.CharacterId);
                });

            migrationBuilder.CreateTable(
                name: "character_history",
                columns: table => new
                {
                    EntryId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_history", x => x.EntryId);
                });

            migrationBuilder.CreateTable(
                name: "character_record",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    OwnerUserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    BaseRuleSystem = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    MixedModeEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverlaySourcesJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_record", x => x.CharacterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_draft_OwnerUserId",
                table: "character_draft",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_character_draft_UpdatedAtUtc",
                table: "character_draft",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_character_history_CharacterId_TimestampUtc",
                table: "character_history",
                columns: new[] { "CharacterId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_character_record_OwnerUserId",
                table: "character_record",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_character_record_UpdatedAtUtc",
                table: "character_record",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_draft");

            migrationBuilder.DropTable(
                name: "character_history");

            migrationBuilder.DropTable(
                name: "character_record");
        }
    }
}
