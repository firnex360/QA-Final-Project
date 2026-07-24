using System.Net;
using Integration.Fixtures;

namespace Integration.SecurityTests;

public class JwtValidationTests : IClassFixture<InventoryApiFactory>
{
    private readonly InventoryApiFactory _factory;

    public JwtValidationTests(InventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_WithNoToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Unauthenticated", "true");

        // Act
        var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithGarbageToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid");

        // Act
        var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithEmptyBearerToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer ");

        // Act
        var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
