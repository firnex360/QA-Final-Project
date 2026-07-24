using System.Net;
using Integration.Fixtures;

namespace Integration.SecurityTests;

public class CorsValidationTests : IClassFixture<InventoryApiFactory>
{
    private readonly InventoryApiFactory _factory;

    public CorsValidationTests(InventoryApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_FromAllowedOrigin_IncludesCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/product");
        request.Headers.Add("Origin", "http://localhost:5167");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); // 204 NoContent for preflight
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:5167", response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
    }

    [Fact]
    public async Task Request_FromDisallowedOrigin_NoCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/product");
        request.Headers.Add("Origin", "http://unauthorized-evil-site.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        // For unauthorized CORS origin preflight, ASP.NET Core does not include Access-Control-Allow-Origin
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
