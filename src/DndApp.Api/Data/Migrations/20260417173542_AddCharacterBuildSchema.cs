using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterBuildSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_sheet",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    BaseRuleSystem = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    BuildMethod = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    ClassModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ProficiencyBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_sheet", x => x.CharacterId);
                });

            migrationBuilder.CreateTable(
                name: "character_ability_score",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    AbilityName = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_ability_score", x => new { x.CharacterId, x.AbilityName });
                    table.ForeignKey(
                        name: "FK_character_ability_score_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_skill_proficiency",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    SkillName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_skill_proficiency", x => new { x.CharacterId, x.SkillName });
                    table.ForeignKey(
                        name: "FK_character_skill_proficiency_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_sheet_ClassModuleId",
                table: "character_sheet",
                column: "ClassModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_ability_score");

            migrationBuilder.DropTable(
                name: "character_skill_proficiency");

            migrationBuilder.DropTable(
                name: "character_sheet");
        }
    }
}
