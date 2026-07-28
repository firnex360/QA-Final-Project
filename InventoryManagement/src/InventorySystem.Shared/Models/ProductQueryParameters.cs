namespace InventorySystem.Shared.Models;

// #1.0-query-params
// Bound from the query string of GET /api/product (#1.4-list-api).
// Defaults: PageNumber=1, PageSize=10, no search, no filters, sort by Name ascending.
// Consumed by ProductService.GetProductsFilterAsync (#1.4-list-service).

/// <summary>
/// Query parameters for server-side pagination, searching, filtering, and sorting.
/// All properties are optional — defaults return page 1 with 10 items, no filters.
/// </summary>
public class ProductQueryParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Searches across Name, CodeSKU, and Description.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Filter by exact category name.</summary>
    public string? Category { get; set; }

    /// <summary>Column to sort by: Name, Price, Quantity, Category, CodeSKU (default: Name).</summary>
    public string? SortBy { get; set; }

    /// <summary>If true, sort descending; otherwise ascending.</summary>
    public bool SortDescending { get; set; }

    /// <summary>If true, only products at or below their minimum stock level.</summary>
    public bool LowStockOnly { get; set; }
}
