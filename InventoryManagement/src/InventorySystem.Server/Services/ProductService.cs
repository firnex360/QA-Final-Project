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

    public async Task<PagedResponse<Product>> GetProductsFilterAsync(ProductQueryParameters p)
    {
        IQueryable<Product> query = _context.Products;

        // Search across Name, CodeSKU, Description ...
        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var term = p.SearchTerm.ToLower();
            
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(term)) ||
                (x.CodeSKU != null && x.CodeSKU.ToLower().Contains(term)) ||
                (x.Description != null && x.Description.ToLower().Contains(term))
            );

        }

        // Filter by category
        if (!string.IsNullOrWhiteSpace(p.Category))
            query = query.Where(x => x.Category == p.Category);

        // Sorting
        query = (p.SortBy?.ToLower()) switch
        {
            "price"    => p.SortDescending ? query.OrderByDescending(x => x.Price)    : query.OrderBy(x => x.Price),
            "quantity" => p.SortDescending ? query.OrderByDescending(x => x.Quantity)  : query.OrderBy(x => x.Quantity),
            "category" => p.SortDescending ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "codesku"  => p.SortDescending ? query.OrderByDescending(x => x.CodeSKU)  : query.OrderBy(x => x.CodeSKU),
            _          => p.SortDescending ? query.OrderByDescending(x => x.Name)     : query.OrderBy(x => x.Name),
        };

        var totalCount = await query.CountAsync();
        var pageSize = Math.Clamp(p.PageSize, 1, 50);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var currentPage = Math.Clamp(p.PageNumber, 1, Math.Max(totalPages, 1));

        var items = await query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Product>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = currentPage
        };
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