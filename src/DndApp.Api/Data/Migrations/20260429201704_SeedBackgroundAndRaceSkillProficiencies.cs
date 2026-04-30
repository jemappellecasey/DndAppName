using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedBackgroundAndRaceSkillProficiencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed background skill proficiencies for D&D 5e 2014 & 2024 editions
            // Backgrounds typically grant 2 skill proficiencies
            var backgroundProficiencies = new (string, string[])[]
            {
                ("Acolyte", new[] { "Insight", "Religion" }),
                ("Charlatan", new[] { "Deception", "Sleight of Hand" }),
                ("Criminal", new[] { "Deception", "Stealth" }),
                ("Entertainer", new[] { "Acrobatics", "Performance" }),
                ("Folk Hero", new[] { "Animal Handling", "Survival" }),
                ("Gladiator", new[] { "Acrobatics", "Performance" }),
                ("Guild Artisan", new[] { "Insight", "Perception" }),
                ("Guild Merchant", new[] { "Insight", "Persuasion" }),
                ("Hermit", new[] { "Medicine", "Religion" }),
                ("Inheritor", new[] { "History", "Persuasion" }),
                ("Knight", new[] { "History", "Persuasion" }),
                ("Mercenary Veteran", new[] { "Athletics", "Deception" }),
                ("Noble", new[] { "History", "Insight" }),
                ("Outlander", new[] { "Athletics", "Survival" }),
                ("Sage", new[] { "Arcana", "History" }),
                ("Sailor", new[] { "Athletics", "Perception" }),
                ("Soldier", new[] { "Athletics", "Intimidation" }),
                ("Urchin", new[] { "Sleight of Hand", "Stealth" }),
                ("Haunted One", new[] { "Investigation", "Survival" }),
                ("Archaeological Dig Site Worker", new[] { "History", "Investigation" }),
                ("Waterborne", new[] { "Athletics", "Survival" }),
                ("Courtier", new[] { "Insight", "Deception" }),
                ("Faction Agent", new[] { "Insight", "Investigation" }),
                ("Far Traveler", new[] { "Insight", "Perception" }),
                ("Cloistered Scholar", new[] { "History", "Insight" }),
            };

            var sql = new System.Text.StringBuilder();

            // Insert background proficiencies for both editions
            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var (backgroundName, skills) in backgroundProficiencies)
                {
                    foreach (var skill in skills)
                    {
                        var profId = $"background-prof-{edition}-{backgroundName.ToLower().Replace(" ", "-")}-{skill.ToLower().Replace(" ", "-")}";

                        sql.AppendLine($@"
INSERT OR IGNORE INTO skill_proficiency_source (Id, SourceType, SourceId, SourceName, SkillName, IsExpertise, IsChoice, Edition)
VALUES ('{profId}', 'background', '{backgroundName.ToLower()}', '{backgroundName}', '{skill}', 0, 0, '{edition}');");
                    }
                }
            }

            // Seed race/species skill proficiencies
            // D&D 5e 2024 uses "Species" but both have skill benefits
            var raceProficiencies = new (string, string[])[]
            {
                ("Dragonborn", new[] { "Intimidation" }),
                ("Dwarf", new[] { "Insight" }),
                ("Elf", new[] { "Perception" }),
                ("Gnome", new[] { "Arcana" }),
                ("Half-Elf", new[] { "Insight", "Persuasion" }),
                ("Half-Orc", new[] { "Intimidation" }),
                ("Halfling", new[] { "Stealth" }),
                ("Human", new string[] { }), // Humans get versatility, not a specific skill
                ("Tiefling", new[] { "Deception" }),
            };

            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var (raceName, skills) in raceProficiencies)
                {
                    // Skip if no skills
                    if (skills.Length == 0) continue;

                    foreach (var skill in skills)
                    {
                        var profId = $"race-prof-{edition}-{raceName.ToLower().Replace(" ", "-")}-{skill.ToLower().Replace(" ", "-")}";

                        sql.AppendLine($@"
INSERT OR IGNORE INTO skill_proficiency_source (Id, SourceType, SourceId, SourceName, SkillName, IsExpertise, IsChoice, Edition)
VALUES ('{profId}', 'race', '{raceName.ToLower()}', '{raceName}', '{skill}', 0, 0, '{edition}');");
                    }
                }
            }

            migrationBuilder.Sql(sql.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete seeded background and race proficiencies
            migrationBuilder.Sql("DELETE FROM skill_proficiency_source WHERE SourceType IN ('background', 'race');");
        }
    }
}
