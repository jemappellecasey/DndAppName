using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeCatalogSpellTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_spell",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    EditionYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    School = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RangeText = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Duration = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Ritual = table.Column<bool>(type: "INTEGER", nullable: false),
                    Concentration = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_spell", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_ContentSourceId_Slug_EditionYear",
                table: "catalog_spell",
                columns: new[] { "ContentSourceId", "Slug", "EditionYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_EditionYear_Level_Name",
                table: "catalog_spell",
                columns: new[] { "EditionYear", "Level", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_LegacyRuleModuleId",
                table: "catalog_spell",
                column: "LegacyRuleModuleId");

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_spell (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    EditionYear, Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                )
                SELECT
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    2014, Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                FROM catalog_spell_2014;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_spell (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    EditionYear, Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                )
                SELECT
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    2024, Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                FROM catalog_spell_2024;
                """);

            migrationBuilder.DropTable(
                name: "catalog_spell_2014");

            migrationBuilder.DropTable(
                name: "catalog_spell_2024");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_spell_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Concentration = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RangeText = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Ritual = table.Column<bool>(type: "INTEGER", nullable: false),
                    School = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_spell_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_spell_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Concentration = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RangeText = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Ritual = table.Column<bool>(type: "INTEGER", nullable: false),
                    School = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_spell_2024", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2014_ContentSourceId_Slug",
                table: "catalog_spell_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2014_LegacyRuleModuleId",
                table: "catalog_spell_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2014_Level_Name",
                table: "catalog_spell_2014",
                columns: new[] { "Level", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2024_ContentSourceId_Slug",
                table: "catalog_spell_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2024_LegacyRuleModuleId",
                table: "catalog_spell_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2024_Level_Name",
                table: "catalog_spell_2024",
                columns: new[] { "Level", "Name" });

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_spell_2014 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                )
                SELECT
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                FROM catalog_spell
                WHERE EditionYear = 2014;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_spell_2024 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                )
                SELECT
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Level, School, CastingTime, RangeText, Duration,
                    Ritual, Concentration, Description, EditionPayloadJson
                FROM catalog_spell
                WHERE EditionYear = 2024;
                """);

            migrationBuilder.DropTable(
                name: "catalog_spell");
        }
    }
}
