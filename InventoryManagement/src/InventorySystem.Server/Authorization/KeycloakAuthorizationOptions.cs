namespace InventorySystem.Server.Authorization;

/// <summary>
/// Settings for Keycloak Authorization Services (UMA) policy evaluation.
/// Bound from the "Keycloak" configuration section.
/// </summary>
public sealed class KeycloakAuthorizationOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>Realm base URL, e.g. http://localhost:8080/realms/inventory-realm.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Internal URL used for server-to-server calls. In Docker the browser reaches Keycloak
    /// at localhost:8080 while the API must use the container name, so these differ.
    /// Falls back to <see cref="Authority"/> when unset.
    /// </summary>
    public string? InternalAuthority { get; set; }

    /// <summary>The resource server client that owns the Resources/Scopes/Policies/Permissions.</summary>
    public string Audience { get; set; } = "inventory-api";

    /// <summary>
    /// Turns per-request policy enforcement on or off. Disabled in tests so the suite
    /// does not require a running Keycloak.
    /// </summary>
    public bool EnforcementEnabled { get; set; } = true;

    /// <summary>Token endpoint used for UMA decision requests.</summary>
    public string TokenEndpoint =>
        $"{(InternalAuthority ?? Authority).TrimEnd('/')}/protocol/openid-connect/token";
}
