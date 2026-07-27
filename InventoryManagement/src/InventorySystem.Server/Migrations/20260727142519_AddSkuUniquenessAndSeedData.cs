using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventorySystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuUniquenessAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CodeSKU",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CodeSKU", "Description", "IsActive", "MinimumStockLevel", "Name", "Price", "Quantity" },
                values: new object[,]
                {
                    { 1, "Electronics", "LAP-001", "Business laptop, 16 GB RAM.", true, 5, "Laptop Pro 15", 1299.99m, 12 },
                    { 2, "Accessories", "ACC-002", "Bluetooth optical mouse.", true, 10, "Wireless Mouse", 24.50m, 3 },
                    { 3, "Furniture", "FUR-003", "Height-adjustable desk.", true, 4, "Standing Desk", 549.00m, 20 }
                });

            // The seed rows above set Id explicitly, which does NOT move PostgreSQL's id counter.
            // Left alone, the next product the app creates would be handed Id 1 and crash into the
            // seeded row. This pushes the counter past the seed data.
            migrationBuilder.Sql("""
                SELECT setval(pg_get_serial_sequence('"Products"', 'Id'),
                              (SELECT MAX("Id") FROM "Products"));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CodeSKU",
                table: "Products",
                column: "CodeSKU",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CodeSKU",
                table: "Products");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.AlterColumn<string>(
                name: "CodeSKU",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
