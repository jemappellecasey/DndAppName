using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedAutoSpellGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed automatic spell grants for classes, subclasses, and races
            // This migration populates the spell grant tables with core D&D 5e spell assignments
            
            var sql = new System.Text.StringBuilder();

            // ============================================================================
            // CLASS SPELL GRANTS - D&D 5e 2014 & 2024
            // ============================================================================
            
            // Wizard Spellcasting - grants cantrips and 1st level spells at level 1
            // Base Wizard spells at level 1: access to all cantrips in spellbook (4 cantrips)
            var wizardCantrips2014 = new[] { "acid-splash", "fire-bolt", "light", "mage-hand", "mending", "message", "minor-illusion", "prestidigitation", "ray-of-frost", "shocking-grasp" };
            var wizardFirstLevel2014 = new[] { "armor", "burning-hands", "charm-person", "color-spray", "comprehend-languages", "detect-magic", "disguise-self", "expeditious-retreat", "false-life", "feather-fall", "find-familiar", "fog-cloud", "grease", "identify", "illusory-script", "jump", "longstrider", "mage-armor", "magic-missile", "protection-from-good-and-evil", "sanctuary", "shield", "sleep", "tenser-floating-disk", "thunderwave", "unseen-servant", "ventriloquism", "witch-bolt" };

            foreach (var edition in new[] { "2014", "2024" })
            {
                // Wizard cantrips at level 1
                foreach (var spellSlug in wizardCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Wizard Spellcasting'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Wizard' AND s.Slug = '{spellSlug}';");
                }

                // Wizard 1st level spells available for selection
                foreach (var spellSlug in wizardFirstLevel2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Wizard Spellcasting (Selectable)'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Wizard' AND s.Slug = '{spellSlug}';");
                }
            }

            // Cleric cantrips
            var clericCantrips2014 = new[] { "guidance", "light", "mending", "resistance", "sacred-flame", "spare-the-dying", "thaumaturgy" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var spellSlug in clericCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Cleric Spellcasting'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Cleric' AND s.Slug = '{spellSlug}';");
                }
            }

            // Bard cantrips
            var bardCantrips2014 = new[] { "mage-hand", "minor-illusion", "prestidigitation", "vicious-mockery" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var spellSlug in bardCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Bard Spellcasting'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Bard' AND s.Slug = '{spellSlug}';");
                }
            }

            // Sorcerer cantrips
            var sorcererCantrips2014 = new[] { "acid-splash", "fire-bolt", "light", "mage-hand", "mending", "message", "minor-illusion", "prestidigitation", "ray-of-frost", "shocking-grasp" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var spellSlug in sorcererCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Sorcerer Spellcasting'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Sorcerer' AND s.Slug = '{spellSlug}';");
                }
            }

            // Warlock cantrips
            var warlockCantrips2014 = new[] { "chill-touch", "eldritch-blast", "mage-hand", "minor-illusion", "prestidigitation", "true-strike" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var spellSlug in warlockCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Warlock Spellcasting'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Warlock' AND s.Slug = '{spellSlug}';");
                }
            }

            // Druid cantrips
            var druidCantrips2014 = new[] { "druidcraft", "guidance", "mending", "produce-flame", "resistance", "shillelagh" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                foreach (var spellSlug in druidCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO class_spell_grants (Id, ClassId, Edition, SpellId, MinLevel, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  c.Id,
  '{edition}',
  s.Id,
  1,
  'Druid Spellcasting'
FROM class_catalog_{edition.ToLower()} c, spell_entity s
WHERE c.Name = 'Druid' AND s.Slug = '{spellSlug}';");
                }
            }

            // ============================================================================
            // RACE SPELL GRANTS - D&D 5e 2014 & 2024
            // ============================================================================

            // High Elf gets cantrips
            var highElfCantrips2014 = new[] { "fire-bolt", "light", "ray-of-frost", "shocking-grasp" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                var raceTable = edition == "2014" ? "race_catalog_2014" : "race_catalog_2024";
                foreach (var spellSlug in highElfCantrips2014)
                {
                    sql.AppendLine($@"
INSERT OR IGNORE INTO race_spell_grants (Id, RaceOrSpeciesId, Edition, SpellId, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  r.Id,
  '{edition}',
  s.Id,
  'High Elf Cantrips'
FROM {raceTable} r, spell_entity s
WHERE r.Name LIKE '%High Elf%' AND s.Slug = '{spellSlug}';");
                }
            }

            // Tiefling infernal legacy spell
            foreach (var edition in new[] { "2014", "2024" })
            {
                var raceTable = edition == "2014" ? "race_catalog_2014" : "race_catalog_2024";
                sql.AppendLine($@"
INSERT OR IGNORE INTO race_spell_grants (Id, RaceOrSpeciesId, Edition, SpellId, SourceDescription)
SELECT 
  '{Guid.NewGuid():N}',
  r.Id,
  '{edition}',
  s.Id,
  'Tiefling Infernal Legacy'
FROM {raceTable} r, spell_entity s
WHERE r.Name = 'Tiefling' AND s.Slug = 'thaumaturgy';");
            }

            // ============================================================================
            // FEAT SPELL GRANTS - D&D 5e 2014 & 2024
            // ============================================================================

            // Magic Initiate feat grants 2 cantrips and 1 1st-level spell
            var magicInitiateCantrips = new[] { "acid-splash", "chill-touch", "fire-bolt", "light", "mending", "prestidigitation", "ray-of-frost", "shocking-grasp" };
            foreach (var edition in new[] { "2014", "2024" })
            {
                // For now, we'll store Magic Initiate as a "Selection" type
                // This will be handled more carefully when we build the ingestion tool
                
                // Mark that feat grants magic initiate spells (framework in place, data needs ingestion)
                sql.AppendLine($@"
-- Magic Initiate cantrips (2 to choose from list of 8)
-- Framework ready: FeatSpellGrantEntity with GrantType='Selection', SelectionCount=2");
            }

            // Execute all accumulated SQL
            migrationBuilder.Sql(sql.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM class_spell_grants;
                  DELETE FROM race_spell_grants;
                  DELETE FROM background_spell_grants;
                  DELETE FROM origin_spell_grants;
                  DELETE FROM subclass_spell_grants;
                  DELETE FROM feat_spell_grants;");
        }
    }
}
