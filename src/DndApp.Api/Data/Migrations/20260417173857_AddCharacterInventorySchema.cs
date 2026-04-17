using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_inventory_item",
                columns: table => new
                {
                    InventoryItemId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CharacterId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ItemDefinitionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    RequiresAttunement = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEquipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAttuned = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_inventory_item", x => x.InventoryItemId);
                    table.ForeignKey(
                        name: "FK_character_inventory_item_character_sheet_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "character_sheet",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_character_inventory_item_item_definition_ItemDefinitionId",
                        column: x => x.ItemDefinitionId,
                        principalTable: "item_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_inventory_item_CharacterId",
                table: "character_inventory_item",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_character_inventory_item_ItemDefinitionId",
                table: "character_inventory_item",
                column: "ItemDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_inventory_item");
        }
    }
}
