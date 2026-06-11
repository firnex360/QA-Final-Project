namespace InventorySystem.Shared.Models;
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