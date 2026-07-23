using System.Net.Http.Json;
using InventorySystem.Shared.Models;

namespace InventorySystem.Client.Authorization;

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
    private bool _loaded;

    /// <summary>Raised once permissions arrive so components can re-render.</summary>
    public event Action? Changed;

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

    /// <summary>Clears the cache so the next check refetches (e.g. after re-login).</summary>
    public void Reset()
    {
        _granted.Clear();
        _loaded = false;
    }
}
