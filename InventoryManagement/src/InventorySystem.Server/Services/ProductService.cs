using InventorySystem.Server.Data;
using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public class ProductService(ApplicationDbContext context) : IProductService
{
    private readonly ApplicationDbContext _context = context;


    //for testing
    public async Task<Product> CreateProductAsync()
    {
        var product = new Product
        {
            Name = "Test",
            CodeSKU = "TW-001",
            Description = "A hardcoded test product to verify EF Core + PostgreSQL connectivity.",
            Category = "Testing",
            Price = 19.99m,
            Quantity = 100,
            MinimumStockLevel = 10,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }
}