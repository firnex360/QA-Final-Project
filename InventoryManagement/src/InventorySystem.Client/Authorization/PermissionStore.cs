using System.Net.Http.Json;
using InventorySystem.Shared.Models;

namespace InventorySystem.Client.Authorization;

// #5.7-permission-store
// Client-side cache of what the caller may do. Two ways to ask:
//
//   Has(resource, scope)  — for small in-page details (buttons on a card). Uses the
//                           permission list fetched once from /api/permissions/me.
//   CanAsync(path, method)— for page-level access. Asks the server to evaluate a real
//                           API call, so the page never names a resource or a scope
//                           (#5.12-permission-check-api). Answers are cached per call.
//
// If a request fails the answer is "denied": the UI hides rather than optimistically
// showing, and the API enforces regardless.

/// <summary>
/// Holds the permissions Keycloak grants the signed-in user, fetched once from
/// /api/permissions/me. The UI asks this instead of hardcoding role or permission
/// names, so changing a policy in Keycloak changes the UI with no code edit.
///
/// This is presentation only — the API enforces the same policies server-side.
/// </summary>
public sealed class PermissionStore(HttpClient http)
{
    private readonly HashSet<string> _granted = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _checks = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>Raised once permissions arrive so components can re-render.</summary>
    public event Action? Changed;

    /// <summary>
    /// True once the permission set has been fetched. Pages must check this before
    /// acting on <see cref="Has"/>: until the fetch completes every check answers
    /// false, so a page rendered too early would briefly show "access denied" to a
    /// user who actually has the permission.
    /// </summary>
    public bool IsLoaded => _loaded;

    /// <summary>Fetches the permission set once per session.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
            return;

        try
        {
            var permissions = await http.GetFromJsonAsync<List<UserPermissionDto>>("api/permissions/me")
                              ?? [];

            foreach (var permission in permissions)
                foreach (var scope in permission.Scopes)
                    _granted.Add($"{permission.Resource}:{scope}");
        }
        catch
        {
            // Leave the set empty: the UI hides everything, and the API still enforces.
        }
        finally
        {
            _loaded = true;
            Changed?.Invoke();
        }
    }

    /// <summary>True when Keycloak grants the given scope on the given resource.</summary>
    public bool Has(string resource, string scope) => _granted.Contains($"{resource}:{scope}");

    /// <summary>
    /// Asks the server whether the caller could perform an API call, without performing
    /// it. Pages use this so they declare the call they make rather than the permission
    /// that protects it — the resource/scope mapping stays entirely in Keycloak.
    /// Each path+method pair is only asked once per session.
    /// </summary>
    public async Task<bool> CanAsync(string path, string method = "GET")
    {
        var key = $"{method} {path}";

        if (_checks.TryGetValue(key, out var cached))
            return cached;

        bool allowed;
        try
        {
            allowed = await http.GetFromJsonAsync<bool>(
                $"api/permissions/check?path={Uri.EscapeDataString(path)}" +
                $"&method={Uri.EscapeDataString(method)}");
        }
        catch
        {
            // Unreachable API or an unexpected answer: fail closed, same as the server.
            allowed = false;
        }

        _checks[key] = allowed;
        return allowed;
    }

    /// <summary>Clears the cache so the next check refetches (e.g. after re-login).</summary>
    public void Reset()
    {
        _granted.Clear();
        _checks.Clear();
        _loaded = false;
    }
}
