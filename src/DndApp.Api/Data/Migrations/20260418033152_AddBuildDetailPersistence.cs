using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildDetailPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrainingLevel",
                table: "character_skill_proficiency",
                type: "TEXT",
                maxLength: 24,
                nullable: false,
                defaultValue: "Proficient");

            migrationBuilder.CreateTable(
                name: "character_class_level",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClassName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_class_level", x => new { x.CharacterId, x.SortOrder });
                    table.ForeignKey(
                        name: "FK_character_class_level_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_selected_module",
                columns: table => new
                {
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Slot = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SourceCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_selected_module", x => new { x.CharacterId, x.Slot, x.ModuleId });
                    table.ForeignKey(
                        name: "FK_character_selected_module_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_class_level_CharacterId_ClassModuleId",
                table: "character_class_level",
                columns: new[] { "CharacterId", "ClassModuleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_class_level");

            migrationBuilder.DropTable(
                name: "character_selected_module");

            migrationBuilder.DropColumn(
                name: "TrainingLevel",
                table: "character_skill_proficiency");
        }
    }
}
