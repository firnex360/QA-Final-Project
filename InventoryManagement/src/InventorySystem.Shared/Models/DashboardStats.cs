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
    public decimal TotalInventoryValue { get; set; }
    public List<LabelCountDto> ByCategory { get; set; } = new();
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
