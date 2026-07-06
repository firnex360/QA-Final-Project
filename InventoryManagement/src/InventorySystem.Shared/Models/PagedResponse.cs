namespace InventorySystem.Shared.Models;

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
