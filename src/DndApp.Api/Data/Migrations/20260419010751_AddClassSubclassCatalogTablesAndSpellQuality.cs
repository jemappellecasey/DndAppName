using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSubclassCatalogTablesAndSpellQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IngestionConfidence",
                table: "catalog_spell",
                type: "TEXT",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SourceSectionId",
                table: "catalog_spell",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatBlockJson",
                table: "catalog_spell",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "catalog_class_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    HitDie = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    PrimaryAbilityJson = table.Column<string>(type: "TEXT", nullable: false),
                    SavingThrowAbilitiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    SkillProficienciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_class_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_class_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    HitDie = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    PrimaryAbilityJson = table.Column<string>(type: "TEXT", nullable: false),
                    SavingThrowAbilitiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    SkillProficienciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_class_2024", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_subclass_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ParentClassId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SubclassFeatureStartLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_subclass_2014", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_subclass_2014_catalog_class_2014_ParentClassId",
                        column: x => x.ParentClassId,
                        principalTable: "catalog_class_2014",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_subclass_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ParentClassId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SubclassFeatureStartLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_subclass_2024", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_subclass_2024_catalog_class_2024_ParentClassId",
                        column: x => x.ParentClassId,
                        principalTable: "catalog_class_2024",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_SourceSectionId",
                table: "catalog_spell",
                column: "SourceSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_class_2014_ContentSourceId_Slug",
                table: "catalog_class_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_class_2014_LegacyRuleModuleId",
                table: "catalog_class_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_class_2014_Name",
                table: "catalog_class_2014",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_class_2024_ContentSourceId_Slug",
                table: "catalog_class_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_class_2024_LegacyRuleModuleId",
                table: "catalog_class_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_class_2024_Name",
                table: "catalog_class_2024",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_subclass_2014_ContentSourceId_Slug",
                table: "catalog_subclass_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_subclass_2014_ParentClassId",
                table: "catalog_subclass_2014",
                column: "ParentClassId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_subclass_2014_ParentClassId_Name",
                table: "catalog_subclass_2014",
                columns: new[] { "ParentClassId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_subclass_2024_ContentSourceId_Slug",
                table: "catalog_subclass_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_subclass_2024_ParentClassId",
                table: "catalog_subclass_2024",
                column: "ParentClassId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_subclass_2024_ParentClassId_Name",
                table: "catalog_subclass_2024",
                columns: new[] { "ParentClassId", "Name" });

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_class_2014
                (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name, Description, HitDie,
                    PrimaryAbilityJson, SavingThrowAbilitiesJson, SkillProficienciesJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(substr(b.RawText, 1, 4000), ''),
                    CASE
                        WHEN lower(m.DisplayName) LIKE '%wizard%' OR lower(m.DisplayName) LIKE '%sorcerer%' THEN 'd6'
                        WHEN lower(m.DisplayName) LIKE '%barbarian%' THEN 'd12'
                        WHEN lower(m.DisplayName) LIKE '%fighter%' OR lower(m.DisplayName) LIKE '%paladin%' OR lower(m.DisplayName) LIKE '%ranger%' THEN 'd10'
                        ELSE 'd8'
                    END,
                    '[]',
                    '[]',
                    '{}',
                    COALESCE(v.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source cs ON cs.Id = m.ContentSourceId
                LEFT JOIN rule_variant v ON v.RuleModuleId = m.Id AND v.RuleSystemId = cs.RuleSystemId
                LEFT JOIN source_block b ON b.SourceSectionId = json_extract(v.PayloadJson, '$.sourceSectionId')
                WHERE lower(m.ModuleType) = 'class'
                  AND cs.RuleSystemId = 'rules-2014';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_class_2024
                (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name, Description, HitDie,
                    PrimaryAbilityJson, SavingThrowAbilitiesJson, SkillProficienciesJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(substr(b.RawText, 1, 4000), ''),
                    CASE
                        WHEN lower(m.DisplayName) LIKE '%wizard%' OR lower(m.DisplayName) LIKE '%sorcerer%' THEN 'd6'
                        WHEN lower(m.DisplayName) LIKE '%barbarian%' THEN 'd12'
                        WHEN lower(m.DisplayName) LIKE '%fighter%' OR lower(m.DisplayName) LIKE '%paladin%' OR lower(m.DisplayName) LIKE '%ranger%' THEN 'd10'
                        ELSE 'd8'
                    END,
                    '[]',
                    '[]',
                    '{}',
                    COALESCE(v.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source cs ON cs.Id = m.ContentSourceId
                LEFT JOIN rule_variant v ON v.RuleModuleId = m.Id AND v.RuleSystemId = cs.RuleSystemId
                LEFT JOIN source_block b ON b.SourceSectionId = json_extract(v.PayloadJson, '$.sourceSectionId')
                WHERE lower(m.ModuleType) = 'class'
                  AND cs.RuleSystemId = 'rules-2024';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_subclass_2014
                (
                    Id, ContentSourceId, LegacyRuleModuleId, ParentClassId, Slug, Name,
                    SubclassFeatureStartLevel, Description, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    COALESCE(
                        (
                            SELECT c.Id
                            FROM catalog_class_2014 c
                            WHERE lower(c.Name) = (
                                CASE
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%wizard%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%school of%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%arcane tradition%' THEN 'wizard'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%bard%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%college%' THEN 'bard'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%cleric%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%domain%' THEN 'cleric'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%druid%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%circle%' THEN 'druid'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%fighter%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%martial archetype%' THEN 'fighter'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%rogue%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%roguish archetype%' THEN 'rogue'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%sorcerer%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%sorcerous origin%' THEN 'sorcerer'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%warlock%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%patron%' THEN 'warlock'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%monk%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%monastic tradition%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%way of%' THEN 'monk'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%paladin%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%oath%' THEN 'paladin'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%ranger%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%ranger archetype%' THEN 'ranger'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%barbarian%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%path of%' THEN 'barbarian'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%artificer%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%alchemist%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%artillerist%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%armorer%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%battle smith%' THEN 'artificer'
                                    ELSE null
                                END
                            )
                            LIMIT 1
                        ),
                        (SELECT c2.Id FROM catalog_class_2014 c2 ORDER BY c2.Name LIMIT 1)
                    ),
                    m.Slug,
                    m.DisplayName,
                    CASE
                        WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%wizard%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%school of%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%arcane tradition%' THEN 2
                        ELSE 3
                    END,
                    COALESCE(substr(b.RawText, 1, 4000), ''),
                    COALESCE(v.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source cs ON cs.Id = m.ContentSourceId
                LEFT JOIN rule_variant v ON v.RuleModuleId = m.Id AND v.RuleSystemId = cs.RuleSystemId
                LEFT JOIN source_block b ON b.SourceSectionId = json_extract(v.PayloadJson, '$.sourceSectionId')
                WHERE lower(m.ModuleType) = 'subclass'
                  AND cs.RuleSystemId = 'rules-2014';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_subclass_2024
                (
                    Id, ContentSourceId, LegacyRuleModuleId, ParentClassId, Slug, Name,
                    SubclassFeatureStartLevel, Description, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    COALESCE(
                        (
                            SELECT c.Id
                            FROM catalog_class_2024 c
                            WHERE lower(c.Name) = (
                                CASE
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%wizard%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%school of%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%arcane tradition%' THEN 'wizard'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%bard%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%college%' THEN 'bard'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%cleric%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%domain%' THEN 'cleric'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%druid%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%circle%' THEN 'druid'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%fighter%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%martial archetype%' THEN 'fighter'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%rogue%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%roguish archetype%' THEN 'rogue'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%sorcerer%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%sorcerous origin%' THEN 'sorcerer'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%warlock%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%patron%' THEN 'warlock'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%monk%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%monastic tradition%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%way of%' THEN 'monk'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%paladin%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%oath%' THEN 'paladin'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%ranger%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%ranger archetype%' THEN 'ranger'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%barbarian%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%path of%' THEN 'barbarian'
                                    WHEN lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%artificer%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%alchemist%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%artillerist%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%armorer%' OR lower(m.DisplayName || ' ' || COALESCE(v.PayloadJson, '')) LIKE '%battle smith%' THEN 'artificer'
                                    ELSE null
                                END
                            )
                            LIMIT 1
                        ),
                        (SELECT c2.Id FROM catalog_class_2024 c2 ORDER BY c2.Name LIMIT 1)
                    ),
                    m.Slug,
                    m.DisplayName,
                    3,
                    COALESCE(substr(b.RawText, 1, 4000), ''),
                    COALESCE(v.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source cs ON cs.Id = m.ContentSourceId
                LEFT JOIN rule_variant v ON v.RuleModuleId = m.Id AND v.RuleSystemId = cs.RuleSystemId
                LEFT JOIN source_block b ON b.SourceSectionId = json_extract(v.PayloadJson, '$.sourceSectionId')
                WHERE lower(m.ModuleType) = 'subclass'
                  AND cs.RuleSystemId = 'rules-2024';
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog_spell
                SET
                    SourceSectionId = COALESCE(json_extract(EditionPayloadJson, '$.sourceSectionId'), ''),
                    IngestionConfidence = CASE
                        WHEN COALESCE(json_extract(EditionPayloadJson, '$.sourceSectionId'), '') <> '' THEN 0.75
                        ELSE 0.45
                    END,
                    StatBlockJson = json_object(
                        'level', Level,
                        'school', School,
                        'castingTime', CastingTime,
                        'range', RangeText,
                        'duration', Duration,
                        'ritual', Ritual,
                        'concentration', Concentration
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_subclass_2014");

            migrationBuilder.DropTable(
                name: "catalog_subclass_2024");

            migrationBuilder.DropTable(
                name: "catalog_class_2014");

            migrationBuilder.DropTable(
                name: "catalog_class_2024");

            migrationBuilder.DropIndex(
                name: "IX_catalog_spell_SourceSectionId",
                table: "catalog_spell");

            migrationBuilder.DropColumn(
                name: "IngestionConfidence",
                table: "catalog_spell");

            migrationBuilder.DropColumn(
                name: "SourceSectionId",
                table: "catalog_spell");

            migrationBuilder.DropColumn(
                name: "StatBlockJson",
                table: "catalog_spell");
        }
    }
}
