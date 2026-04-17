using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "constraint",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ConstraintJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_constraint", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rule_module",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ModuleType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    VersionTag = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_module", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rule_module_content_source_ContentSourceId",
                        column: x => x.ContentSourceId,
                        principalTable: "content_source",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_definition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RequiresAttunement = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChargesModelJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_definition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_definition_rule_module_RuleModuleId",
                        column: x => x.RuleModuleId,
                        principalTable: "rule_module",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prerequisite",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PredicateJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prerequisite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prerequisite_rule_module_RuleModuleId",
                        column: x => x.RuleModuleId,
                        principalTable: "rule_module",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rule_variant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuleSystemId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CompatibilityTagsJson = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_variant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rule_variant_rule_module_RuleModuleId",
                        column: x => x.RuleModuleId,
                        principalTable: "rule_module",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rule_variant_rule_system_RuleSystemId",
                        column: x => x.RuleSystemId,
                        principalTable: "rule_system",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_effect",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemDefinitionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EffectType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EffectPayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_effect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_effect_item_definition_ItemDefinitionId",
                        column: x => x.ItemDefinitionId,
                        principalTable: "item_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_definition_RuleModuleId",
                table: "item_definition",
                column: "RuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_item_effect_ItemDefinitionId",
                table: "item_effect",
                column: "ItemDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_prerequisite_RuleModuleId",
                table: "prerequisite",
                column: "RuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_rule_module_ContentSourceId_Slug_VersionTag",
                table: "rule_module",
                columns: new[] { "ContentSourceId", "Slug", "VersionTag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rule_variant_RuleModuleId_RuleSystemId",
                table: "rule_variant",
                columns: new[] { "RuleModuleId", "RuleSystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rule_variant_RuleSystemId",
                table: "rule_variant",
                column: "RuleSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "constraint");

            migrationBuilder.DropTable(
                name: "item_effect");

            migrationBuilder.DropTable(
                name: "prerequisite");

            migrationBuilder.DropTable(
                name: "rule_variant");

            migrationBuilder.DropTable(
                name: "item_definition");

            migrationBuilder.DropTable(
                name: "rule_module");
        }
    }
}
