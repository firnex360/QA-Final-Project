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

    /// <summary>
    /// Only used by /api/permissions/me, so we can keep the list of granted permissions
    /// empty rather than duplicating Keycloak's resource/scope matrix here, where it
    /// would silently drift from the realm configuration. A test that needs specific
    /// grants can assign them.
    /// </summary>
    public List<UserPermissionDto> GrantedPermissions { get; set; } = [];

    public Task<List<UserPermissionDto>> GetGrantedPermissionsAsync(
        string accessToken,
        CancellationToken cancellationToken = default) => Task.FromResult(GrantedPermissions);
}
