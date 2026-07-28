namespace InventorySystem.Shared.Authorization;

// #5.4-resources-scopes
// The Keycloak Resource and Scope names the UI needs to reason about, kept in Shared so
// client and server spell them identically. Resources map to URI patterns in Keycloak:
//   Products     → /api/product/*      ProductStats → /api/product/stats
//   ProductStock → /api/product/*/stock  Audit      → /api/audit/*
// This is deliberately NOT a permission registry — new permissions or policies created
// in Keycloak need no entry here; a name is added only when the UI must render for it.

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

    // #5.12-scope-convention
    // The single definition of the verb → scope rule. Used by the enforcement
    // middleware (#5.2-policy-middleware) and by the pre-flight check endpoint
    // (#5.12-permission-check-api), so the UI can never assume a different rule
    // than the one actually enforced.
    public static string ForMethod(string? httpMethod) => (httpMethod ?? string.Empty).ToUpperInvariant() switch
    {
        "GET" or "HEAD" => View,
        "DELETE" => Delete,
        _ => Manage
    };
}
