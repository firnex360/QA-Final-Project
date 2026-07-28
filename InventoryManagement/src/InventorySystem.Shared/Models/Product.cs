namespace InventorySystem.Shared.Models;

// #1.0-product-model
// The product entity. Shared by client and server, and mapped straight to the
// "Products" table by EF Core, so this class defines the database columns too.
//
// Fields required by the spec: Name, CodeSKU, Description, Category, Price,
// Quantity (initial stock), MinimumStockLevel, IsActive (estado activo/inactivo).
// Validation rules live in the API (#1.1-create-api / #1.2-edit-api), not here.
    public class Product
    {
        public int Id { get; set;}
        public string? Name { get; set; }
        public string? CodeSKU { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int MinimumStockLevel { get; set; }
        public bool IsActive { get; set; }
    }