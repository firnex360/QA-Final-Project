using InventorySystem.Server.Authorization;
using InventorySystem.Shared.Authorization;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Server.Controllers;

// #5.6-permissions-api
// Lets the UI discover what it may do, straight from Keycloak, so the client keeps no
// permission list of its own. Both endpoints are exempt from policy enforcement (see
// #5.2-policy-middleware): they report only the caller's own access, so requiring a
// permission to read them would be circular.

/// <summary>
/// Lets the UI discover what the signed-in user may do, straight from Keycloak.
/// This is why the client needs no permission list of its own.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PermissionsController(IAuthorizationDecisionService decisions) : ControllerBase
{
    private const string BearerPrefix = "Bearer ";

    // GET api/permissions/me
    // Exempt from policy enforcement (see PolicyEnforcementMiddleware): it exposes only
    // the caller's own grants, so requiring a permission to read it would be circular.
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var header = Request.Headers.Authorization.ToString();
        var token = header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..].Trim()
            : string.Empty;

        var permissions = await decisions.GetGrantedPermissionsAsync(token, cancellationToken);
        return Ok(permissions);
    }

    // #5.12-permission-check-api
    // GET /api/permissions/check?path=/api/product&method=POST  ->  true | false
    //
    // Pre-flight: answers whether the caller could perform an API call, WITHOUT
    // performing it. The UI uses this to decide whether to render a page, so a page
    // declares the call it makes ("POST /api/product") instead of naming a resource or
    // scope — those live only in Keycloak.
    //
    // Returns a bare boolean on purpose: the answer is one bit, so it needs no DTO.
    // The scope is derived with the same convention the enforcement middleware uses
    // (#5.12-scope-convention) and evaluated by the same service (#5.3-keycloak-decision),
    // so the answer here can never disagree with what the real request would do.
    [HttpGet("check")]
    [Authorize]
    public async Task<IActionResult> Check(
        [FromQuery] string? path,
        [FromQuery] string? method,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("A 'path' query parameter is required.");

        // Only this API's own paths can be evaluated; anything else is a client mistake.
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only '/api/...' paths can be checked.");

        var header = Request.Headers.Authorization.ToString();
        var token = header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..].Trim()
            : string.Empty;

        // Lowercased to match how the middleware normalises paths before asking Keycloak.
        var decision = await decisions.EvaluateAsync(
            token, path.ToLowerInvariant(), Scopes.ForMethod(method), cancellationToken);

        return Ok(decision == AuthorizationDecision.Allowed);
    }
}
