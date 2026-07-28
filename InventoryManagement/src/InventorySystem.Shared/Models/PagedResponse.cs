namespace InventorySystem.Shared.Models;

// #1.0-paged-response
// Envelope returned by GET /api/product (#1.4-list-api). The three metadata fields
// (TotalCount, TotalPages, CurrentPage) are what the UI pager renders (#1.4.4-pagination-ui).

/// <summary>
/// Results from the server for a paginated query, including the items and metadata about the pagination.
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
