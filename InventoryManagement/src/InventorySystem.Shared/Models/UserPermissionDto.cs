namespace InventorySystem.Shared.Models;

/// <summary>
/// A resource the caller may access, with the scopes they hold on it.
/// Mirrors what Keycloak Authorization Services grants — nothing is defined in code.
/// </summary>
public class UserPermissionDto
{
    /// <summary>Keycloak Resource name, e.g. "Products".</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>Scopes granted on that resource, e.g. ["view", "manage"].</summary>
    public List<string> Scopes { get; set; } = [];
}
