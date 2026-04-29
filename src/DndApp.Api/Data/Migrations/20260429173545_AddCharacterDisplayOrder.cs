using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "character_record",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Initialize DisplayOrder based on CreatedAtUtc for existing records
            // Active characters: ordered by creation date (newest first)
            migrationBuilder.Sql(
                """
                UPDATE character_record
                SET DisplayOrder = (
                    SELECT COUNT(*) FROM character_record cr2
                    WHERE cr2.OwnerUserId = character_record.OwnerUserId
                      AND cr2.IsArchived = 0
                      AND cr2.CreatedAtUtc > character_record.CreatedAtUtc
                )
                WHERE IsArchived = 0;
                """);

            // Archived characters: don't sort by DisplayOrder, keep by UpdatedAtUtc
            // Set DisplayOrder to 0 for all archived (they won't be sorted by this field)
            migrationBuilder.Sql(
                """
                UPDATE character_record
                SET DisplayOrder = 0
                WHERE IsArchived = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "character_record");
        }
    }
}
