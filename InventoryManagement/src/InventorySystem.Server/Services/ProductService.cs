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

    // Superseded by GetProductsFilterAsync; kept only because it is part of IProductService.
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    // #1.4-list-service
    // Builds the product list query for GET /api/product (#1.4-list-api).
    //
    // Everything is composed onto a single IQueryable and executed once, so searching,
    // filtering, sorting and paging all run in SQL on the database — never in memory.
    // Order matters: narrow the rows first (search → filters), then sort, then page.
    //
    // Limits applied here: PageSize is clamped to 1..50 and PageNumber is clamped to
    // the available range, so hand-edited query strings can't request 10,000 rows or
    // a page that doesn't exist.
    public async Task<PagedResponse<Product>> GetProductsFilterAsync(ProductQueryParameters p)
    {
        IQueryable<Product> query = _context.Products;

        // #1.4.1-search
        // Case-insensitive "contains" across Name, CodeSKU and Description.
        // Both sides are lowered so the match is case-insensitive regardless of DB collation.
        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var term = p.SearchTerm.ToLower();

            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(term)) ||
                (x.CodeSKU != null && x.CodeSKU.ToLower().Contains(term)) ||
                (x.Description != null && x.Description.ToLower().Contains(term))
            );
        }

        // #1.4.2-filters
        // Two independent, combinable filters:
        //   Category     → exact match on the category name
        //   LowStockOnly → only products at or below their minimum stock level
        if (!string.IsNullOrWhiteSpace(p.Category))
            query = query.Where(x => x.Category == p.Category);

        if (p.LowStockOnly)
            query = query.Where(x => x.Quantity <= x.MinimumStockLevel);

        // #1.4.3-sorting
        // Accepted SortBy values: price, quantity, category, codesku — anything else
        // (including null) falls through to Name. SortDescending flips the direction.
        query = (p.SortBy?.ToLower()) switch
        {
            "price"    => p.SortDescending ? query.OrderByDescending(x => x.Price)    : query.OrderBy(x => x.Price),
            "quantity" => p.SortDescending ? query.OrderByDescending(x => x.Quantity)  : query.OrderBy(x => x.Quantity),
            "category" => p.SortDescending ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "codesku"  => p.SortDescending ? query.OrderByDescending(x => x.CodeSKU)  : query.OrderBy(x => x.CodeSKU),
            _          => p.SortDescending ? query.OrderByDescending(x => x.Name)     : query.OrderBy(x => x.Name),
        };

        // #1.4.4-pagination
        // TotalCount is counted AFTER search/filters so the pager reflects the filtered
        // set. PageSize is capped at 50; CurrentPage is clamped into range, which is why
        // deleting the last row on the last page safely falls back instead of 404-ing.
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

    // #4.1-dashboard-stats
    // Computes the dashboard figures in one pass over the catalogue:
    //   Totals      — total / active / inactive products
    //   Alerts      — LowStockCount (Quantity <= MinimumStockLevel) and OutOfStockCount
    //                 (Quantity == 0). NOTE: low stock INCLUDES out of stock.
    //   Money       — TotalInventoryValue and ValueByCategory (sum of Price × Quantity)
    //   Breakdowns  — ByCategory (product counts per category)
    //   Critical    — the 8 most urgent products, ranked by how far below their minimum
    //                 they are; this is the "productos críticos" watchlist.
    public async Task<ProductStatsDto> GetProductStatsAsync()
    {
        var products = await _context.Products.ToListAsync();
        return new ProductStatsDto
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive),
            InactiveProducts = products.Count(p => !p.IsActive),
            LowStockCount = products.Count(p => p.Quantity <= p.MinimumStockLevel),
            OutOfStockCount = products.Count(p => p.Quantity == 0),
            TotalInventoryValue = products.Sum(p => p.Price * p.Quantity),
            ByCategory = products
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Uncategorized" : p.Category!)
                .Select(g => new LabelCountDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            ValueByCategory = products
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Uncategorized" : p.Category!)
                .Select(g => new LabelCountDto { Label = g.Key, Count = (int)Math.Round(g.Sum(p => p.Price * p.Quantity)) })
                .OrderByDescending(x => x.Count)
                .ToList(),
            // Most critical first: the further below its minimum, the higher it ranks.
            CriticalProducts = products
                .Where(p => p.Quantity <= p.MinimumStockLevel)
                .OrderBy(p => p.Quantity - p.MinimumStockLevel)
                .ThenBy(p => p.Quantity)
                .Take(8)
                .Select(p => new LowStockItemDto
                {
                    Name = p.Name ?? "(unnamed)",
                    CodeSKU = p.CodeSKU,
                    Category = p.Category,
                    Quantity = p.Quantity,
                    MinimumStockLevel = p.MinimumStockLevel
                })
                .ToList()
        };
    }

    public async Task UpdateProductAsync(Product product)
    {
        await _context.SaveChangesAsync();
    }
    
    // #2.1-adjust-service
    // Applies a stock movement: positive delta = stock in, negative = stock out.
    // Guard rail: the resulting quantity may never go below zero — the attempt throws
    // InvalidOperationException (surfaced as 400) and nothing is saved.
    // No movement table is written here; SaveChangesAsync triggers the audit interceptor
    // (#2.4-audit-config), and the movement history is derived from it (#2.3-movements-derive).
    public async Task<Product?> AdjustStockAsync(int id, int delta)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return null;

        var newQuantity = product.Quantity + delta;
        if (newQuantity < 0)
            throw new InvalidOperationException(
                $"Stock cannot go below zero. Current quantity is {product.Quantity}, requested change is {delta}.");

        product.Quantity = newQuantity;
        await _context.SaveChangesAsync();

        return product;
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