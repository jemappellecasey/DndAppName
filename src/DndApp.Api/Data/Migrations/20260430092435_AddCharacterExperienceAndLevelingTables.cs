using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterExperienceAndLevelingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create table to track character experience
            migrationBuilder.CreateTable(
                name: "character_experience",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    TotalExperience = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    ExperienceForNextLevel = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 300L),
                    AbilityScoreImprovementsUsed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LastLevelUpAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_experience", x => x.CharacterId);
                });

            // Create table to track level progression milestones
            migrationBuilder.CreateTable(
                name: "character_level_progression",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceRequired = table.Column<long>(type: "INTEGER", nullable: false),
                    LeveledUpAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GrantedAbilityScoreImprovement = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    GrantedFeatOption = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FeatOrASIChosenJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_level_progression", x => x.Id);
                });

            // Create indexes for efficient querying
            migrationBuilder.CreateIndex(
                name: "IX_character_experience_current_level",
                table: "character_experience",
                column: "CurrentLevel");

            migrationBuilder.CreateIndex(
                name: "IX_character_level_progression_character_id",
                table: "character_level_progression",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_character_level_progression_level",
                table: "character_level_progression",
                column: "Level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_level_progression");

            migrationBuilder.DropTable(
                name: "character_experience");
        }
    }
}
