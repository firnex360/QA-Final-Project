namespace InventorySystem.Shared.Models;

/// <summary>A generic label/count pair used for chart series (category, action, user, day...).</summary>
public class LabelCountDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Aggregated product figures for the broad (home) dashboard.</summary>
public class ProductStatsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int LowStockCount { get; set; }

    /// <summary>Products with zero quantity on hand.</summary>
    public int OutOfStockCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<LabelCountDto> ByCategory { get; set; } = new();

    /// <summary>Inventory value (price × quantity) per category, rounded to whole currency units.</summary>
    public List<LabelCountDto> ValueByCategory { get; set; } = new();

    /// <summary>Products at or below their minimum stock level — the "critical" watchlist.</summary>
    public List<LowStockItemDto> CriticalProducts { get; set; } = new();
}

/// <summary>A single at-risk product shown in the dashboard's critical-stock list.</summary>
public class LowStockItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? CodeSKU { get; set; }
    public string? Category { get; set; }
    public int Quantity { get; set; }
    public int MinimumStockLevel { get; set; }
}

/// <summary>Aggregated audit figures for the admin (audit) dashboard.</summary>
public class AuditStatsDto
{
    public int TotalEvents { get; set; }
    public int EventsToday { get; set; }
    public int EventsThisWeek { get; set; }
    public List<LabelCountDto> ByAction { get; set; } = new();
    public List<LabelCountDto> ByUser { get; set; } = new();
    public List<LabelCountDto> ByDay { get; set; } = new();
    public List<LabelCountDto> ByEntity { get; set; } = new();
}
