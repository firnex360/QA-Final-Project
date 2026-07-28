using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Authorization;

/// <summary>
/// Outcome of asking Keycloak whether a request is permitted.
/// </summary>
public enum AuthorizationDecision
{
    
    Allowed, ///Keycloak evaluated the policies and permitted the request.
    Denied, /// A resource matched, but its policies denied the request.
    NoResourceDefined /// No Keycloak resource matches the request path.
}

/// <summary>
/// Asks the authorization server whether a caller may perform a scope on a path.
/// Abstracted so tests can substitute a fake instead of calling a live Keycloak.
/// </summary>
public interface IAuthorizationDecisionService
{
    Task<AuthorizationDecision> EvaluateAsync(
        string accessToken,
        string path,
        string scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every resource/scope pair the caller currently holds. Used by the UI to decide
    /// what to render, so the client never needs its own copy of the permission model.
    /// </summary>
    Task<List<UserPermissionDto>> GetGrantedPermissionsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
