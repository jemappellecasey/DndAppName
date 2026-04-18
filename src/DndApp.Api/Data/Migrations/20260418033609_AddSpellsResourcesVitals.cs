using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpellsResourcesVitals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_resource_pool",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ResourceKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurrentValue = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxValue = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_resource_pool", x => new { x.CharacterId, x.ResourceKey });
                    table.ForeignKey(
                        name: "FK_character_resource_pool_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_spell_entry",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    SpellModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PreparationMode = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    SpellName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_spell_entry", x => new { x.CharacterId, x.SpellModuleId, x.PreparationMode });
                    table.ForeignKey(
                        name: "FK_character_spell_entry_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_vitals",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MaxHitPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentHitPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    TempHitPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseMoveSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseArmorClass = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_vitals", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_character_vitals_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_resource_pool");

            migrationBuilder.DropTable(
                name: "character_spell_entry");

            migrationBuilder.DropTable(
                name: "character_vitals");
        }
    }
}
