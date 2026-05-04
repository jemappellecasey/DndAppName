using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_AutomaticSpellGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "background_spell_grant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BackgroundId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    SpellId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_spell_grant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_spell_grant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClassId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    SpellId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MinLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAlwaysPrepared = table.Column<bool>(type: "INTEGER", nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_spell_grant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "feat_spell_grant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FeatId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    GrantType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SpellIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SelectionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feat_spell_grant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "origin_spell_grant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OriginId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SpellId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_origin_spell_grant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "race_spell_grant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RaceOrSpeciesId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    SpellId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_race_spell_grant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subclass_spell_grant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SubclassId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    SpellId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MinLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subclass_spell_grant", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_background_spell_grant_BackgroundId_Edition",
                table: "background_spell_grant",
                columns: new[] { "BackgroundId", "Edition" });

            migrationBuilder.CreateIndex(
                name: "IX_class_spell_grant_ClassId_Edition",
                table: "class_spell_grant",
                columns: new[] { "ClassId", "Edition" });

            migrationBuilder.CreateIndex(
                name: "IX_feat_spell_grant_FeatId_Edition",
                table: "feat_spell_grant",
                columns: new[] { "FeatId", "Edition" });

            migrationBuilder.CreateIndex(
                name: "IX_origin_spell_grant_OriginId",
                table: "origin_spell_grant",
                column: "OriginId");

            migrationBuilder.CreateIndex(
                name: "IX_race_spell_grant_RaceOrSpeciesId_Edition",
                table: "race_spell_grant",
                columns: new[] { "RaceOrSpeciesId", "Edition" });

            migrationBuilder.CreateIndex(
                name: "IX_subclass_spell_grant_SubclassId_Edition",
                table: "subclass_spell_grant",
                columns: new[] { "SubclassId", "Edition" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "background_spell_grant");

            migrationBuilder.DropTable(
                name: "class_spell_grant");

            migrationBuilder.DropTable(
                name: "feat_spell_grant");

            migrationBuilder.DropTable(
                name: "origin_spell_grant");

            migrationBuilder.DropTable(
                name: "race_spell_grant");

            migrationBuilder.DropTable(
                name: "subclass_spell_grant");
        }
    }
}
