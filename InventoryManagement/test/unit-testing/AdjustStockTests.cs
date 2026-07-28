using InventorySystem.Server.Controllers;
using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_testing;

/// <summary>
/// Tests for PATCH api/product/{id}/stock?delta=N — stock entries and exits.
/// A positive delta is stock coming in, a negative delta is stock going out.
/// </summary>
public class AdjustStockTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly ProductController _controller;

    public AdjustStockTests()
    {
        _mockService = new Mock<IProductService>();
        _controller = new ProductController(_mockService.Object);
    }

    private static Product Product(int quantity) => new()
    {
        Id = 5,
        Name = "Webcam",
        CodeSKU = "WC-700",
        Description = "1080p webcam",
        Category = "Peripherals",
        Price = 49.99m,
        Quantity = quantity,
        MinimumStockLevel = 3,
        IsActive = true
    };

    [Fact]
    public async Task AdjustStock_PositiveDelta_ReturnsOkWithUpdatedProduct()
    {
        // Arrange — 20 in stock, 5 coming in
        _mockService
            .Setup(s => s.AdjustStockAsync(5, 5))
            .ReturnsAsync(Product(25));

        // Act
        var result = await _controller.AdjustStock(5, 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(25, updated.Quantity);
        _mockService.Verify(s => s.AdjustStockAsync(5, 5), Times.Once);
    }

    [Fact]
    public async Task AdjustStock_NegativeDelta_ReturnsOkWithUpdatedProduct()
    {
        // Arrange — 20 in stock, 8 going out
        _mockService
            .Setup(s => s.AdjustStockAsync(5, -8))
            .ReturnsAsync(Product(12));

        // Act
        var result = await _controller.AdjustStock(5, -8);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(12, updated.Quantity);
        _mockService.Verify(s => s.AdjustStockAsync(5, -8), Times.Once);
    }

    [Fact]
    public async Task AdjustStock_ZeroDelta_ReturnsBadRequest()
    {
        // Arrange
        // nothing — the controller rejects this before reaching the service

        // Act
        var result = await _controller.AdjustStock(5, 0);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Delta cannot be zero.", badRequest.Value);
        _mockService.Verify(s => s.AdjustStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AdjustStock_NonExistingId_ReturnsNotFound()
    {
        // Arrange — the service returns null when there is no such product
        _mockService
            .Setup(s => s.AdjustStockAsync(999, 5))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _controller.AdjustStock(999, 5);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Taking out more than is on hand is refused: the service throws and the endpoint reports it
    /// as a bad request rather than letting stock go negative.
    /// </summary>
    [Fact]
    public async Task AdjustStock_MoreThanAvailable_ReturnsBadRequestWithReason()
    {
        // Arrange — only 20 on hand, 50 requested out
        _mockService
            .Setup(s => s.AdjustStockAsync(5, -50))
            .ThrowsAsync(new InvalidOperationException(
                "Stock cannot go below zero. Current quantity is 20, requested change is -50."));

        // Act
        var result = await _controller.AdjustStock(5, -50);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(
            "Stock cannot go below zero. Current quantity is 20, requested change is -50.",
            badRequest.Value);
    }
}
