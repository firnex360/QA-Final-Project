using System.Net;
using System.Net.Http.Json;
using Integration.Fixtures;
using InventorySystem.Shared.Models;

namespace Integration.Tests;

/// <summary>
/// Integration tests for the audit-derived stock movement history
/// (api/audit/stock-movements). Verifies that adjusting a product's stock produces a
/// movement entry, sourced entirely from the audit trail (no stock-movement table).
/// </summary>
public class StockMovementApiTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;

    public StockMovementApiTests(InventoryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStockMovements_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/audit/stock-movements", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStockMovementStats_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/audit/stock-movements/stats", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StockMovements_ContainsEntry_AfterStockAdjusted()
    {
        // Arrange — create a product to adjust.
        var product = new Product
        {
            Name = "Movement Test Product",
            CodeSKU = $"SKU-MOVE-{Guid.NewGuid().ToString()[..6]}",
            Description = "Created to verify stock movement history.",
            Category = "MoveTest",
            Price = 10.00m,
            Quantity = 20,
            MinimumStockLevel = 1,
            IsActive = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/product", product, cancellationToken: TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProductCreatedResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        // Act — remove 4 units (a stock-out). This produces a Product Update audit row.
        var patch = await _client.PatchAsync(
            $"/api/product/{created!.ProductId}/stock?delta=-4",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);
        patch.EnsureSuccessStatusCode();

        var movements = await _client.GetFromJsonAsync<List<StockMovementDto>>(
            "/api/audit/stock-movements", cancellationToken: TestContext.Current.CancellationToken);

        // Assert — the movement is derived from the audit trail with the right delta.
        Assert.NotNull(movements);
        Assert.Contains(movements!, m =>
            m.ProductId == created.ProductId &&
            m.PreviousQuantity == 20 &&
            m.NewQuantity == 16 &&
            m.Delta == -4 &&
            m.ProductName == "Movement Test Product");
    }

    private sealed class ProductCreatedResponse
    {
        public string Message { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
    }
}
