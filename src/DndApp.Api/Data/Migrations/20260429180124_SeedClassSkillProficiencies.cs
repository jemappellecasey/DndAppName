using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedClassSkillProficiencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed class skill proficiencies for D&D 5e 2014 & 2024 editions
            // Based on Player's Handbook skill proficiency lists per class
            
            // All editions can use the same proficiency list as they're the same in 5e
            var skillProficienciesPerClass = new[]
            {
                ("Artificer", new[] { "Arcana", "Investigation", "Medicine", "Nature", "Sleight of Hand" }),
                ("Bard", new[] { "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception", "History", "Insight", "Investigation", "Medicine", "Nature", "Perception", "Performance", "Persuasion", "Religion", "Sleight of Hand", "Stealth", "Survival" }),
                ("Barbarian", new[] { "Animal Handling", "Athletics", "Insight", "Intimidation", "Nature", "Perception", "Survival" }),
                ("Cleric", new[] { "Insight", "Medicine", "Persuasion", "Religion" }),
                ("Druid", new[] { "Arcana", "Animal Handling", "Insight", "Medicine", "Nature", "Perception", "Survival" }),
                ("Fighter", new[] { "Acrobatics", "Animal Handling", "Athletics", "History", "Insight", "Intimidation", "Perception", "Survival" }),
                ("Monk", new[] { "Acrobatics", "Animal Handling", "Athletics", "History", "Insight", "Religion", "Stealth" }),
                ("Paladin", new[] { "Athletics", "Insight", "Intimidation", "Medicine", "Persuasion", "Religion" }),
                ("Ranger", new[] { "Animal Handling", "Athletics", "Insight", "Investigation", "Nature", "Perception", "Stealth", "Survival" }),
                ("Rogue", new[] { "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception", "History", "Insight", "Investigation", "Medicine", "Nature", "Perception", "Performance", "Persuasion", "Religion", "Sleight of Hand", "Stealth", "Survival" }),
                ("Sorcerer", new[] { "Arcana", "Deception", "Insight", "Intimidation", "Persuasion", "Religion" }),
                ("Warlock", new[] { "Arcana", "Deception", "History", "Insight", "Intimidation", "Investigation", "Nature", "Religion" }),
                ("Wizard", new[] { "Arcana", "History", "Insight", "Investigation", "Medicine", "Religion" })
            };

            // Insert for both 2014 and 2024 editions
            var sql = new System.Text.StringBuilder();
            var id = 1;

            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var (className, skills) in skillProficienciesPerClass)
                {
                    foreach (var skill in skills)
                    {
                        var profId = $"class-prof-{edition}-{className.ToLower()}-{skill.ToLower().Replace(" ", "-")}";
                        
                        sql.AppendLine($@"
INSERT INTO skill_proficiency_source (Id, SourceType, SourceId, SourceName, SkillName, IsExpertise, IsChoice, Edition)
VALUES ('{profId}', 'class', '{className.ToLower()}', '{className}', '{skill}', 0, 0, '{edition}');");
                        id++;
                    }
                }
            }

            migrationBuilder.Sql(sql.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete all seeded class proficiencies
            migrationBuilder.Sql("DELETE FROM skill_proficiency_source WHERE SourceType = 'class';");
        }
    }
}
