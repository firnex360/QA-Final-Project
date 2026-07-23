using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using InventorySystem.Server.Authorization;
using System.Text.Json;

namespace InventorySystem.Server.Controllers;

/// <summary>
/// Proxies Keycloak's token endpoint so API testers can obtain a valid JWT
/// by sending username/password credentials without needing the browser-based
/// OAuth2 login flow.
///
/// Usage:
///   POST /api/auth/token  { "username": "staff", "password": "staff123" }
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
    private readonly KeycloakAuthorizationOptions _keycloak;

    public AuthController(
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakAuthorizationOptions> keycloakOptions)
    {
        _httpClientFactory = httpClientFactory;
        _keycloak = keycloakOptions.Value;
    }

    /// <summary>
    /// Exchange Keycloak username/password for a JWT access token.
    /// This endpoint is NOT secured — it is how you obtain a token and its only going to work on development.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToken([FromBody] LoginRequest request)
    {
        // Use InternalAuthority (keycloak:8080 in Docker) for the server-to-server call.
        // The issued token's issuer will still be localhost:8080 thanks to KC_HOSTNAME.
        var tokenUrl = _keycloak.TokenEndpoint;

        // Build the form-encoded request for the Resource Owner Password Credentials grant.
        // We authenticate against inventory-client (the public client the user logs into).
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "inventory-client",
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["scope"] = "openid profile email"
        };

        HttpResponseMessage response;
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new
            {
                error = "keycloak_unreachable",
                message = $"Could not connect to Keycloak at {tokenUrl}",
                details = ex.Message
            });
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return StatusCode((int)response.StatusCode, new
                {
                    error = "keycloak_error",
                    message = $"Keycloak returned {(int)response.StatusCode} with no body",
                    token_url = tokenUrl
                });
            }

            try
            {
                var errorObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return StatusCode((int)response.StatusCode, errorObj);
            }
            catch (JsonException)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    error = "keycloak_error",
                    message = responseContent
                });
            }
        }

        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        return Ok(tokenResponse);
    }
}

public record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}
