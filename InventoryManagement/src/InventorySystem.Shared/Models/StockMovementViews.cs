namespace InventorySystem.Shared.Models;

/// <summary>
/// A single stock movement, derived on the fly from the audit log: one "Product" Update
/// whose Quantity column changed. This is a read-only projection over AuditLogs — there is
/// no stock-movement table; the data lives entirely in the existing audit trail.
/// </summary>
public class StockMovementDto
{
    /// <summary>Id of the underlying audit record this movement came from.</summary>
    public int AuditId { get; set; }

    /// <summary>The affected product's primary key (audit EntityId).</summary>
    public int ProductId { get; set; }

    /// <summary>Product name resolved from the Products table (falls back to "Product #id").</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Usuario — who made the change (audit UserId).</summary>
    public string? UserId { get; set; }

    /// <summary>Fecha — when the change happened (UTC).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Cantidad anterior — quantity before the change.</summary>
    public int PreviousQuantity { get; set; }

    /// <summary>Cantidad nueva — quantity after the change.</summary>
    public int NewQuantity { get; set; }

    /// <summary>Signed change: positive = stock in (entrada), negative = stock out (salida).</summary>
    public int Delta { get; set; }
}

/// <summary>Aggregated stock-movement figures for the dashboard, all derived from the audit log.</summary>
public class StockMovementStatsDto
{
    public int TotalMovements { get; set; }
    public int MovementsThisWeek { get; set; }

    /// <summary>Total units that entered stock (sum of positive deltas).</summary>
    public int TotalUnitsIn { get; set; }

    /// <summary>Total units that left stock (sum of negative deltas, as a positive number).</summary>
    public int TotalUnitsOut { get; set; }

    /// <summary>Movement count split by direction (In vs Out).</summary>
    public List<LabelCountDto> ByType { get; set; } = new();

    /// <summary>Movement count per day over the last 7 days.</summary>
    public List<LabelCountDto> ByDay { get; set; } = new();

    /// <summary>Most sold products — ranked by units removed from stock (Out movements).</summary>
    public List<LabelCountDto> TopProducts { get; set; } = new();
}
