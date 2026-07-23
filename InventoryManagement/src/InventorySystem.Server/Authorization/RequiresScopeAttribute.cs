namespace InventorySystem.Server.Authorization;

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
