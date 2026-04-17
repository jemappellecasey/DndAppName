using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRawSourceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "source_book",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    VersionTag = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_book", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "source_chapter",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceBookId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ChapterOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_chapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_source_chapter_source_book_SourceBookId",
                        column: x => x.SourceBookId,
                        principalTable: "source_book",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "source_section",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceChapterId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    SectionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    StartLine = table.Column<int>(type: "INTEGER", nullable: false),
                    EndLine = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_section", x => x.Id);
                    table.ForeignKey(
                        name: "FK_source_section_source_chapter_SourceChapterId",
                        column: x => x.SourceChapterId,
                        principalTable: "source_chapter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "source_block",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceSectionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BlockType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RawText = table.Column<string>(type: "TEXT", nullable: false),
                    ParseConfidence = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_block", x => x.Id);
                    table.ForeignKey(
                        name: "FK_source_block_source_section_SourceSectionId",
                        column: x => x.SourceSectionId,
                        principalTable: "source_section",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_source_block_SourceSectionId",
                table: "source_block",
                column: "SourceSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_source_book_SourceCode_VersionTag",
                table: "source_book",
                columns: new[] { "SourceCode", "VersionTag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_chapter_SourceBookId_ChapterOrder",
                table: "source_chapter",
                columns: new[] { "SourceBookId", "ChapterOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_section_SourceChapterId_SectionOrder",
                table: "source_section",
                columns: new[] { "SourceChapterId", "SectionOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "source_block");

            migrationBuilder.DropTable(
                name: "source_section");

            migrationBuilder.DropTable(
                name: "source_chapter");

            migrationBuilder.DropTable(
                name: "source_book");
        }
    }
}
