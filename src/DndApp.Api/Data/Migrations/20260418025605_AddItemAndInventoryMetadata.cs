using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAndInventoryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttackBonus",
                table: "item_definition",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DamageBonus",
                table: "item_definition",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DamageDice",
                table: "item_definition",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "GoldValue",
                table: "item_definition",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsWeapon",
                table: "item_definition",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WeaponAbility",
                table: "item_definition",
                type: "TEXT",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "item_definition",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "character_inventory_item",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttackBonus",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "DamageBonus",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "DamageDice",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "GoldValue",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "IsWeapon",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "WeaponAbility",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "item_definition");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "character_inventory_item");
        }
    }
}
