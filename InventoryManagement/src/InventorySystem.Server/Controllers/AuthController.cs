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
        // Use the internal token URL if configured (needed in Docker where
        // "localhost" doesn't reach the Keycloak container), otherwise fall
        // back to building it from the Authority URL.
        var tokenUrl = _configuration["Keycloak:TokenUrl"];
        if (string.IsNullOrEmpty(tokenUrl))
        {
            var authority = _configuration["Keycloak:Authority"]
                ?? "http://localhost:8080/realms/inventory-realm";
            tokenUrl = $"{authority}/protocol/openid-connect/token";
        }

        var clientId = _configuration["Keycloak:Audience"]
            ?? "inventory-client";

        // Build the form-encoded request for the Resource Owner Password Credentials grant
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
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
            // Keycloak is unreachable (wrong host, container not started, etc.)
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
            // Forward Keycloak's error — handle empty or non-JSON responses gracefully
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
                // Keycloak returned non-JSON (e.g. HTML error page)
                return StatusCode((int)response.StatusCode, new
                {
                    error = "keycloak_error",
                    message = responseContent
                });
            }
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
