using InventorySystem.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace InventorySystem.Server.Authorization;

/// <summary>
/// Enforces Keycloak-defined policies on every API request.
///
/// The HTTP method determines the authorization scope (read vs write) and the request
/// path identifies the resource, so protecting a new endpoint means creating a Resource
/// in Keycloak — no code change here.
/// </summary>
public sealed class PolicyEnforcementMiddleware(
    RequestDelegate next,
    IOptions<KeycloakAuthorizationOptions> options,
    ILogger<PolicyEnforcementMiddleware> logger)
{
    private readonly KeycloakAuthorizationOptions _options = options.Value;

    // Infrastructure endpoints that are never subject to the permission model.
    private static readonly string[] ExemptPrefixes =
    [
        "/metrics",
        "/openapi",
        "/scalar",
        "/health",
        // Reports only the caller's own grants; gating it on a permission would be circular.
        "/api/permissions"
    ];

    public async Task InvokeAsync(HttpContext context, IAuthorizationDecisionService decisions)
    {
        if (!_options.EnforcementEnabled || !RequiresEnforcement(context))
        {
            await next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            // Authentication middleware normally catches this first; this is a safety net.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Present on every real request; empty only under test authentication schemes.
        var token = ExtractBearerToken(context) ?? string.Empty;
        var path = (context.Request.Path.Value ?? "/").ToLowerInvariant();
        var scope = ScopeForRequest(context);

        var decision = await decisions.EvaluateAsync(token, path, scope, context.RequestAborted);

        switch (decision)
        {
            case AuthorizationDecision.Allowed:
                await next(context);
                return;

            case AuthorizationDecision.NoResourceDefined:
                // Fail closed, but say so plainly — this is a configuration gap, not a
                // permission problem, and the two are easy to confuse when debugging.
                logger.LogWarning(
                    "Denied {Method} {Path}: no Keycloak Resource matches this URI. "
                    + "Create a Resource with this URI (scope '{Scope}') in the '{Audience}' client.",
                    context.Request.Method, path, scope, _options.Audience);
                await WriteForbidden(context, $"No authorization resource is defined for '{path}'.");
                return;

            default:
                logger.LogInformation("Denied {Method} {Path}: policy evaluation refused scope '{Scope}'.",
                    context.Request.Method, path, scope);
                await WriteForbidden(context, $"You do not have permission to '{scope}' this resource.");
                return;
        }
    }

    /// <summary>Only authenticated API calls are evaluated; [AllowAnonymous] opts out.</summary>
    private static bool RequiresEnforcement(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (ExemptPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return false;

        // CORS preflight carries no credentials and must not be blocked.
        if (HttpMethods.IsOptions(context.Request.Method))
            return false;

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return false;

        return true;
    }

    /// <summary>
    /// Resolves the scope to ask Keycloak for. An explicit [RequiresScope] wins;
    /// otherwise the HTTP verb decides: reads are "view", deletes are "delete"
    /// (so removal can be restricted separately from editing), and the remaining
    /// write verbs are "manage".
    /// </summary>
    private static string ScopeForRequest(HttpContext context)
    {
        var declared = context.GetEndpoint()?.Metadata.GetMetadata<RequiresScopeAttribute>();
        if (declared is not null)
            return declared.Scope;

        var method = context.Request.Method;

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
            return Scopes.View;

        if (HttpMethods.IsDelete(method))
            return Scopes.Delete;

        return Scopes.Manage;
    }

    private static string? ExtractBearerToken(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;
    }

    private static async Task WriteForbidden(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new { error = "forbidden", message },
            cancellationToken: context.RequestAborted);
    }
}
