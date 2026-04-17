using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rule_system",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_system", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_source",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RuleSystemId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_source", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_source_rule_system_RuleSystemId",
                        column: x => x.RuleSystemId,
                        principalTable: "rule_system",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_source_Code",
                table: "content_source",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_source_RuleSystemId",
                table: "content_source",
                column: "RuleSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_rule_system_Name",
                table: "rule_system",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_source");

            migrationBuilder.DropTable(
                name: "rule_system");
        }
    }
}
