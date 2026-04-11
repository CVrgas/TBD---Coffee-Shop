using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryLedgeCorruption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Delta",
                table: "StockMovements",
                newName: "ReservedDelta");

            migrationBuilder.AddColumn<int>(
                name: "QuantityDelta",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityDelta",
                table: "StockMovements");

            migrationBuilder.RenameColumn(
                name: "ReservedDelta",
                table: "StockMovements",
                newName: "Delta");
        }
    }
}
