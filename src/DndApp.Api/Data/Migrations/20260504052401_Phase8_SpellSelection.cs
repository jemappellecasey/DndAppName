using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_SpellSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "character_record",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "character_experience",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalExperience = table.Column<long>(type: "INTEGER", nullable: false),
                    ExperienceForNextLevel = table.Column<long>(type: "INTEGER", nullable: false),
                    AbilityScoreImprovementsUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    LastLevelUpAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_experience", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_character_experience_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_level_progression",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceRequired = table.Column<long>(type: "INTEGER", nullable: false),
                    LeveledUpAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GrantedAbilityScoreImprovement = table.Column<bool>(type: "INTEGER", nullable: false),
                    GrantedFeatOption = table.Column<bool>(type: "INTEGER", nullable: false),
                    FeatOrASIChosenJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_level_progression", x => x.Id);
                    table.ForeignKey(
                        name: "FK_character_level_progression_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_levelup_choice",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ChoiceType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ChosenAbility = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true),
                    ChosenFeatId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_levelup_choice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_character_levelup_choice_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillProficiencySources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    SourceId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", nullable: false),
                    SkillName = table.Column<string>(type: "TEXT", nullable: false),
                    IsExpertise = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsChoice = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChoiceGroup = table.Column<string>(type: "TEXT", nullable: true),
                    Edition = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillProficiencySources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_level_progression_CharacterId_Level",
                table: "character_level_progression",
                columns: new[] { "CharacterId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_character_levelup_choice_CharacterId_Level_IsConfirmed",
                table: "character_levelup_choice",
                columns: new[] { "CharacterId", "Level", "IsConfirmed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_experience");

            migrationBuilder.DropTable(
                name: "character_level_progression");

            migrationBuilder.DropTable(
                name: "character_levelup_choice");

            migrationBuilder.DropTable(
                name: "SkillProficiencySources");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "character_record");
        }
    }
}
