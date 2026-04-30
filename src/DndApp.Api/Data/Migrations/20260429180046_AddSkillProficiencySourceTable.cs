using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillProficiencySourceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create table to track skill proficiency sources per class/race/background/feat
            migrationBuilder.CreateTable(
                name: "skill_proficiency_source",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SourceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SkillName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsExpertise = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsChoice = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ChoiceGroup = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_proficiency_source", x => x.Id);
                });

            // Create indexes for efficient querying
            migrationBuilder.CreateIndex(
                name: "IX_skill_proficiency_source_edition",
                table: "skill_proficiency_source",
                column: "Edition");

            migrationBuilder.CreateIndex(
                name: "IX_skill_proficiency_source_skill_name",
                table: "skill_proficiency_source",
                column: "SkillName");

            migrationBuilder.CreateIndex(
                name: "IX_skill_proficiency_source_source_id",
                table: "skill_proficiency_source",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_proficiency_source_source_type",
                table: "skill_proficiency_source",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_proficiency_source");
        }
    }
}
