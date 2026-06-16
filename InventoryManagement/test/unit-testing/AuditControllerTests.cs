using InventorySystem.Server.Controllers;
using InventorySystem.Server.Data;
using InventorySystem.Server.Models;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace unit_testing;

/// <summary>
/// Tests for GET api/audit — retrieve audit logs.
/// </summary>
public class AuditControllerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuditController _controller;

    public AuditControllerTests()
    {
        // Setup an In-Memory database for testing EF Core directly
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _controller = new AuditController(_dbContext);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsOk_WithRecentLogsOrderedByTimestampDescending()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-2);
        var recentDate = DateTime.UtcNow;

        _dbContext.AuditLogs.AddRange(
            new AuditLog { Id = 1, EntityName = "Product", Action = "Create", Timestamp = oldDate, UserId = "User1" },
            new AuditLog { Id = 2, EntityName = "Product", Action = "Update", Timestamp = recentDate, UserId = "User2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAuditLogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLogs = Assert.IsType<List<AuditLogDto>>(okResult.Value);
        
        Assert.Equal(2, returnedLogs.Count);
        // Ensure they are ordered descending (most recent first)
        Assert.Equal(2, returnedLogs[0].Id); 
        Assert.Equal(1, returnedLogs[1].Id);
    }

    [Fact]
    public async Task GetAuditLogs_CapsAt100Records()
    {
        // Arrange
        var logsToInsert = new List<AuditLog>();
        for (int i = 1; i <= 105; i++)
        {
            logsToInsert.Add(new AuditLog 
            { 
                Id = i, 
                EntityName = "Product", 
                Action = "Update", 
                Timestamp = DateTime.UtcNow.AddMinutes(i), 
                UserId = "User1" 
            });
        }
        
        _dbContext.AuditLogs.AddRange(logsToInsert);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAuditLogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedLogs = Assert.IsType<List<AuditLogDto>>(okResult.Value);
        
        Assert.Equal(100, returnedLogs.Count);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
