using System.Net;
using System.Net.Http.Json;
using Integration.Fixtures;
using InventorySystem.Shared.Models;

namespace Integration.Tests;

/// <summary>
/// Integration tests for the Audit API (api/audit).
/// Verifies that audit log entries are automatically created
/// when products are created/modified via the API.
/// </summary>
public class AuditApiTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;

    public AuditApiTests(InventoryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/audit", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ContainsInsertEntry_AfterProductCreated()
    {
        // Arrange
        var product = new Product
        {
            Name = "Audit Test Product",
            CodeSKU = $"SKU-AUDIT-{Guid.NewGuid().ToString()[..6]}",
            Description = "Created to verify audit logging.",
            Category = "AuditTest",
            Price = 10.00m,
            Quantity = 5,
            MinimumStockLevel = 1,
            IsActive = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/product", product, cancellationToken: TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();

        // Act 
        var response = await _client.GetAsync("/api/audit", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogDto>>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert 
        Assert.NotNull(logs);
        Assert.Contains(logs, log =>
            log.EntityName == "Product" &&
            log.Action == "Insert");
    }
}
