using System.Net;
using System.Net.Http.Json;
using Integration.Fixtures;

namespace Integration.SecurityTests;

public class AuthenticationFlowTests : IClassFixture<InventoryApiFactory>
{
    private readonly InventoryApiFactory _factory;

    public AuthenticationFlowTests(InventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthToken_Endpoint_IsAnonymous_DoesNotReturn401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Unauthenticated", "true");

        // Act
        // Calling token endpoint with invalid JSON body to see if it reaches controller (returns 400 Bad Request) instead of 401 Unauthorized
        var response = await client.PostAsJsonAsync<object>("/api/auth/token", new { Username = "", Password = "" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthToken_WithEmptyCredentials_ReturnsBadOrGatewayError()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/token", new { Username = "", Password = "" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        // Should not crash with 500, but will return 502 BadGateway, 400, 401, or 403 when auth endpoint fails without live Keycloak
        Assert.True(response.StatusCode == HttpStatusCode.BadGateway || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden);
    }
}
