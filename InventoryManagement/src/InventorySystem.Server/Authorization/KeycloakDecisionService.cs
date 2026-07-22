using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using InventorySystem.Shared.Models;
using Microsoft.Extensions.Options;

namespace InventorySystem.Server.Authorization;

/// <summary>
/// Evaluates each request against Keycloak Authorization Services using the UMA
/// ticket grant. Keycloak matches the request path against the URIs defined on its
/// Resources, then runs the Permissions/Policies bound to them — so the whole
/// authorization matrix lives in Keycloak, not in this codebase.
///
/// Only the caller's own access token is sent; no client secret is required.
/// </summary>
public sealed class KeycloakDecisionService(
    HttpClient httpClient,
    IOptions<KeycloakAuthorizationOptions> options,
    ILogger<KeycloakDecisionService> logger) : IAuthorizationDecisionService
{
    private readonly KeycloakAuthorizationOptions _options = options.Value;

    public async Task<AuthorizationDecision> EvaluateAsync(
        string accessToken,
        string path,
        string scope,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // "permission" is "<uri>#<scope>"; the two permission_resource_* parameters tell
        // Keycloak to match that URI against its Resource definitions.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
            ["audience"] = _options.Audience,
            ["permission"] = $"{path}#{scope}",
            ["permission_resource_format"] = "uri",
            ["permission_resource_matching_uri"] = "true",
            ["response_mode"] = "decision"
        });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            // Keycloak unreachable — fail closed rather than letting the request through.
            logger.LogError(ex, "Authorization server unreachable while evaluating {Scope} on {Path}.", scope, path);
            return AuthorizationDecision.Denied;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return ParseDecision(body, path, scope);

            if (response.StatusCode == HttpStatusCode.BadRequest && body.Contains("invalid_resource"))
                return AuthorizationDecision.NoResourceDefined;

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
                return AuthorizationDecision.Denied;

            logger.LogWarning(
                "Unexpected authorization response {Status} for {Scope} on {Path}: {Body}",
                (int)response.StatusCode, scope, path, body);
            return AuthorizationDecision.Denied;
        }
    }

    public async Task<List<UserPermissionDto>> GetGrantedPermissionsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Omitting "permission" asks Keycloak for everything the caller is granted.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
            ["audience"] = _options.Audience,
            ["response_mode"] = "permissions"
        });

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Could not read granted permissions ({Status}): {Body}",
                    (int)response.StatusCode, body);
                return [];
            }

            using var json = JsonDocument.Parse(body);
            var permissions = new List<UserPermissionDto>();

            foreach (var entry in json.RootElement.EnumerateArray())
            {
                var name = entry.TryGetProperty("rsname", out var rs) ? rs.GetString() : null;
                if (string.IsNullOrEmpty(name))
                    continue;

                var scopes = entry.TryGetProperty("scopes", out var s) && s.ValueKind == JsonValueKind.Array
                    ? s.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [];

                permissions.Add(new UserPermissionDto { Resource = name, Scopes = scopes });
            }

            return permissions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read granted permissions from the authorization server.");
            return [];
        }
    }

    private AuthorizationDecision ParseDecision(string body, string path, string scope)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("result", out var result) && result.GetBoolean()
                ? AuthorizationDecision.Allowed
                : AuthorizationDecision.Denied;
        }
        catch (JsonException)
        {
            logger.LogWarning("Could not parse authorization decision for {Scope} on {Path}: {Body}", scope, path, body);
            return AuthorizationDecision.Denied;
        }
    }
}
