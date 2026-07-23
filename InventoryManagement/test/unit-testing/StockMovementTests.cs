using System.Text.Json;
using InventorySystem.Server.Controllers;
using InventorySystem.Server.Data;
using InventorySystem.Server.Models;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace unit_testing;

/// <summary>
/// Tests for the stock-movement history derived from the audit log
/// (GET api/audit/stock-movements[/stats]). No stock-movement table is involved —
/// these verify the filtering/parsing of AuditLogs.
/// </summary>
public class StockMovementTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuditController _controller;

    public StockMovementTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _controller = new AuditController(_dbContext);
    }

    private static AuditLog QuantityUpdate(int id, string entityId, int oldQty, int newQty, DateTime when, string user = "u1") => new()
    {
        Id = id,
        EntityName = "Product",
        EntityId = entityId,
        Action = "Update",
        Timestamp = when,
        UserId = user,
        OldValues = JsonSerializer.Serialize(new { Quantity = oldQty }),
        NewValues = JsonSerializer.Serialize(new { Quantity = newQty }),
        AffectedColumns = JsonSerializer.Serialize(new[] { "Quantity" })
    };

    [Fact]
    public async Task GetStockMovements_ReturnsOnlyQuantityChanges_Newest_First()
    {
        _dbContext.Products.Add(new Product { Id = 1, Name = "Widget", CodeSKU = "W-1", Description = "d", Category = "c", Price = 1m, Quantity = 12, MinimumStockLevel = 1, IsActive = true });
        _dbContext.AuditLogs.AddRange(
            QuantityUpdate(1, "1", 10, 15, DateTime.UtcNow.AddHours(-2)),        // stock in
            QuantityUpdate(2, "1", 15, 12, DateTime.UtcNow),                     // stock out (newer)
            // A price-only update must NOT show up as a stock movement.
            new AuditLog
            {
                Id = 3, EntityName = "Product", EntityId = "1", Action = "Update", Timestamp = DateTime.UtcNow.AddHours(-1),
                UserId = "u1", OldValues = "{\"Price\":1}", NewValues = "{\"Price\":2}", AffectedColumns = "[\"Price\"]"
            },
            // A non-Product entity must NOT show up.
            new AuditLog { Id = 4, EntityName = "Order", EntityId = "9", Action = "Update", Timestamp = DateTime.UtcNow, AffectedColumns = "[\"Quantity\"]", OldValues = "{\"Quantity\":1}", NewValues = "{\"Quantity\":2}" }
        );
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetStockMovements();

        var ok = Assert.IsType<OkObjectResult>(result);
        var movements = Assert.IsType<List<StockMovementDto>>(ok.Value);

        Assert.Equal(2, movements.Count);               // only the two Quantity changes on the Product
        Assert.Equal(2, movements[0].AuditId);          // newest first
        Assert.Equal(-3, movements[0].Delta);           // 15 -> 12 is a stock-out of 3
        Assert.Equal("Widget", movements[0].ProductName);
        Assert.Equal(5, movements[1].Delta);            // 10 -> 15 is a stock-in of 5
    }

    [Fact]
    public async Task GetStockMovements_FallsBackToId_WhenProductDeleted()
    {
        // No matching product row → name falls back to "Product #id".
        _dbContext.AuditLogs.Add(QuantityUpdate(1, "42", 5, 3, DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetStockMovements();

        var ok = Assert.IsType<OkObjectResult>(result);
        var movements = Assert.IsType<List<StockMovementDto>>(ok.Value);
        Assert.Single(movements);
        Assert.Equal("Product #42", movements[0].ProductName);
        Assert.Equal(42, movements[0].ProductId);
    }

    [Fact]
    public async Task GetStockMovementStats_ComputesUnitsAndMostSold()
    {
        _dbContext.Products.AddRange(
            new Product { Id = 1, Name = "Popular", CodeSKU = "P-1", Description = "d", Category = "c", Price = 1m, Quantity = 0, MinimumStockLevel = 1, IsActive = true },
            new Product { Id = 2, Name = "Niche", CodeSKU = "N-1", Description = "d", Category = "c", Price = 1m, Quantity = 0, MinimumStockLevel = 1, IsActive = true });
        _dbContext.AuditLogs.AddRange(
            QuantityUpdate(1, "1", 100, 90, DateTime.UtcNow),  // Popular out 10
            QuantityUpdate(2, "1", 90, 85, DateTime.UtcNow),   // Popular out 5
            QuantityUpdate(3, "2", 20, 18, DateTime.UtcNow),   // Niche out 2
            QuantityUpdate(4, "1", 85, 135, DateTime.UtcNow)   // Popular in 50
        );
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetStockMovementStats();

        var ok = Assert.IsType<OkObjectResult>(result);
        var stats = Assert.IsType<StockMovementStatsDto>(ok.Value);

        Assert.Equal(4, stats.TotalMovements);
        Assert.Equal(50, stats.TotalUnitsIn);
        Assert.Equal(17, stats.TotalUnitsOut);              // 10 + 5 + 2
        Assert.Equal("Popular", stats.TopProducts.First().Label);
        Assert.Equal(15, stats.TopProducts.First().Count);  // most units out
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
