using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InventorySystem.Server.Controllers;

/// <summary>
/// Proxies Keycloak's token endpoint so API testers can obtain a valid JWT
/// by sending username/password credentials without needing the browser-based
/// OAuth2 login flow.
///
/// Usage:
///   POST /api/auth/token  { "username": "staff", "password": "12345" }
///   → returns { access_token, token_type, expires_in, ... }
///
/// Then use the access_token as:
///   Authorization: Bearer {access_token}
/// on all other API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Exchange Keycloak username/password for a JWT access token.
    /// This endpoint is NOT secured — it is how you obtain a token.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToken([FromBody] LoginRequest request)
    {
        var authority = _configuration["Keycloak:Authority"]
            ?? "http://localhost:8080/realms/inventory-realm";
        var clientId = _configuration["Keycloak:Audience"]
            ?? "inventory-client";

        var tokenUrl = $"{authority}/protocol/openid-connect/token";

        // Build the form-encoded request for the Resource Owner Password Credentials grant
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["scope"] = "openid profile email"
        };

        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Forward Keycloak's error as-is so the tester can see what went wrong
            return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(responseContent));
        }

        // Return the full Keycloak token response (access_token, refresh_token, expires_in, etc.)
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        return Ok(tokenResponse);
    }
}

/// <summary>
/// Request body for the token endpoint.
/// </summary>
public record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}
