namespace InventorySystem.Shared.Authorization;

/// <summary>
/// Names of the Keycloak Authorization Services Resources this application surfaces.
/// These must match the Resource names in Keycloak exactly.
///
/// This is NOT a permission registry: new permissions or policies created in Keycloak
/// need no entry here. Only add a name when the UI has to render something for it.
/// </summary>
public static class Resources
{
    public const string Products = "Products";
    public const string ProductStats = "ProductStats";
    public const string ProductStock = "ProductStock";
    public const string Audit = "Audit";
}

/// <summary>
/// Authorization scopes. The middleware derives these from the HTTP verb
/// (GET/HEAD → view, DELETE → delete, everything else → manage); an endpoint can
/// override that with [RequiresScope] when the verb doesn't express the intent.
/// </summary>
public static class Scopes
{
    public const string View = "view";
    public const string Manage = "manage";
    public const string Delete = "delete";
}
