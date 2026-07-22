using InventorySystem.Server.Authorization;
using InventorySystem.Shared.Models;

namespace Integration.Fixtures;

/// <summary>
/// Stands in for Keycloak policy evaluation so the suite runs without a live
/// authorization server. Defaults to permitting everything; individual tests can
/// set <see cref="NextDecision"/> to assert the middleware's denial behaviour.
/// </summary>
public sealed class FakeAuthorizationDecisionService : IAuthorizationDecisionService
{
    public AuthorizationDecision NextDecision { get; set; } = AuthorizationDecision.Allowed;

    public Task<AuthorizationDecision> EvaluateAsync(
        string accessToken,
        string path,
        string scope,
        CancellationToken cancellationToken = default) => Task.FromResult(NextDecision);

    /// <summary>Full access by default, mirroring an admin's grants.</summary>
    public List<UserPermissionDto> GrantedPermissions { get; set; } =
    [
        new() { Resource = "Products", Scopes = ["view", "manage"] },
        new() { Resource = "ProductStats", Scopes = ["view"] },
        new() { Resource = "ProductStock", Scopes = ["manage"] },
        new() { Resource = "Audit", Scopes = ["view"] }
    ];

    public Task<List<UserPermissionDto>> GetGrantedPermissionsAsync(
        string accessToken,
        CancellationToken cancellationToken = default) => Task.FromResult(GrantedPermissions);
}
