using InventorySystem.Server.Data;
using InventorySystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Server.Services;

public class ProductService(ApplicationDbContext context) : IProductService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<ProductStatsDto> GetProductStatsAsync()
    {
        var products = await _context.Products.ToListAsync();
        return new ProductStatsDto
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive),
            InactiveProducts = products.Count(p => !p.IsActive),
            LowStockCount = products.Count(p => p.Quantity <= p.MinimumStockLevel),
            TotalInventoryValue = products.Sum(p => p.Price * p.Quantity),
            ByCategory = products
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Uncategorized" : p.Category!)
                .Select(g => new LabelCountDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList()
        };
    }

    public async Task UpdateProductAsync(Product product)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteProductByIdAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
    
}