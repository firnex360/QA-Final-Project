using System.Net;
using System.Net.Http.Json;
using Integration.Fixtures;
using InventorySystem.Server.Authorization;
using InventorySystem.Shared.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.SecurityTests;

public class PermissionValidationTests : IClassFixture<InventoryApiFactory>
{
    private readonly InventoryApiFactory _factory;
    private readonly FakeAuthorizationDecisionService _decisionService;

    public PermissionValidationTests(InventoryApiFactory factory)
    {
        _factory = factory;
        _decisionService = factory.Services.GetRequiredService<FakeAuthorizationDecisionService>();
    }

    [Fact]
    public async Task AuthenticatedUser_WithAllowedPermission_Returns200()
    {
        // Arrange
        _decisionService.NextDecision = AuthorizationDecision.Allowed;
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            _decisionService.NextDecision = AuthorizationDecision.Allowed;
        }
    }

    [Fact]
    public async Task AuthenticatedUser_WithDeniedPermission_Returns403()
    {
        // Arrange
        _decisionService.NextDecision = AuthorizationDecision.Denied;
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _decisionService.NextDecision = AuthorizationDecision.Allowed;
        }
    }

    [Fact]
    public async Task DeniedResponse_ContainsForbiddenErrorMessage()
    {
        // Arrange
        _decisionService.NextDecision = AuthorizationDecision.Denied;
        var client = _factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/api/product", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ForbiddenResponseBody>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(body);
            Assert.Equal("forbidden", body.Error);
            Assert.Contains("permission", body.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            _decisionService.NextDecision = AuthorizationDecision.Allowed;
        }
    }

    private record ForbiddenResponseBody(string Error, string Message);
}
