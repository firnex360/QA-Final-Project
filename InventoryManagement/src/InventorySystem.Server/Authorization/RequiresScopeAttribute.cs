namespace InventorySystem.Server.Authorization;

// #5.5-requires-scope
// Escape hatch for endpoints whose HTTP verb doesn't express the intent (e.g. a POST
// that approves rather than creates). Applying it overrides the verb→scope convention
// in #5.2-policy-middleware. The scope must exist on the matching Keycloak Resource,
// otherwise the request is denied (fail closed).

/// <summary>
/// Overrides the scope the policy middleware asks Keycloak for on this endpoint.
///
/// The default convention (GET/HEAD → view, DELETE → delete, otherwise → manage)
/// covers ordinary CRUD. Use this only when the HTTP verb doesn't express the
/// authorization intent — for example a POST that approves rather than creates:
///
///     [HttpPost("{id:int}/approve")]
///     [RequiresScope("approve")]
///
/// The scope must exist on the matching Resource in Keycloak, otherwise the request
/// is denied (fail closed).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiresScopeAttribute(string scope) : Attribute
{
    public string Scope { get; } = scope;
}
