namespace InventorySystem.Shared.Models;

public class AuditLogDto
{
    public int Id { get; set; }

    /// <summary>The name of the entity/table that was changed (e.g., "Product").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>The primary key value of the affected record.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>The type of operation: "Insert", "Update", or "Delete".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>When the change occurred (UTC).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>The user who made the change (from Keycloak JWT). Null if unauthenticated.</summary>
    public string? UserId { get; set; }

    /// <summary>JSON snapshot of the entity's values before the change. Null for Insert.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the entity's values after the change. Null for Delete.</summary>
    public string? NewValues { get; set; }

    /// <summary>JSON array of column names that were modified. Only populated for Update.</summary>
    public string? AffectedColumns { get; set; }
}
