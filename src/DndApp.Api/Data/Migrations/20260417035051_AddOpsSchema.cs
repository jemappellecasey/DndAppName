using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingestion_run",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    VersionTag = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_run", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "import_report",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IngestionRunId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReportJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_report", x => x.Id);
                    table.ForeignKey(
                        name: "FK_import_report_ingestion_run_IngestionRunId",
                        column: x => x.IngestionRunId,
                        principalTable: "ingestion_run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_queue",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IngestionRunId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    QueueType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ReferenceId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Confidence = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_queue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_queue_ingestion_run_IngestionRunId",
                        column: x => x.IngestionRunId,
                        principalTable: "ingestion_run",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "correction_override",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReviewQueueId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OverrideJson = table.Column<string>(type: "TEXT", nullable: false),
                    AppliedBy = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_correction_override", x => x.Id);
                    table.ForeignKey(
                        name: "FK_correction_override_review_queue_ReviewQueueId",
                        column: x => x.ReviewQueueId,
                        principalTable: "review_queue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_correction_override_ReviewQueueId",
                table: "correction_override",
                column: "ReviewQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_import_report_IngestionRunId",
                table: "import_report",
                column: "IngestionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_run_SourceCode_VersionTag_StartedAtUtc",
                table: "ingestion_run",
                columns: new[] { "SourceCode", "VersionTag", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_review_queue_IngestionRunId_Status",
                table: "review_queue",
                columns: new[] { "IngestionRunId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "correction_override");

            migrationBuilder.DropTable(
                name: "import_report");

            migrationBuilder.DropTable(
                name: "review_queue");

            migrationBuilder.DropTable(
                name: "ingestion_run");
        }
    }
}
