using InventorySystem.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Server.Controllers;

/// <summary>
/// Lets the UI discover what the signed-in user may do, straight from Keycloak.
/// This is why the client needs no permission list of its own.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PermissionsController(IAuthorizationDecisionService decisions) : ControllerBase
{
    // GET api/permissions/me
    // Exempt from policy enforcement (see PolicyEnforcementMiddleware): it exposes only
    // the caller's own grants, so requiring a permission to read it would be circular.
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var header = Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : string.Empty;

        var permissions = await decisions.GetGrantedPermissionsAsync(token, cancellationToken);
        return Ok(permissions);
    }
}
