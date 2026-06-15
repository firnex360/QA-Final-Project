using InventorySystem.Server.Controllers;
using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_testing;

/// <summary>
/// Tests for DELETE api/product/{id} — delete a product by ID.
/// </summary>
public class DeleteProductTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly ProductController _controller;

    public DeleteProductTests()
    {
        _mockService = new Mock<IProductService>();
        _controller = new ProductController(_mockService.Object);
    }

    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsOkWithMessage()
    {
        // Arrange
        var product = new Product
        {
            Id = 7,
            Name = "Webcam",
            CodeSKU = "WC-700",
            Description = "1080p webcam",
            Category = "Peripherals",
            Price = 49.99m,
            Quantity = 20,
            MinimumStockLevel = 3,
            IsActive = true
        };

        _mockService
            .Setup(s => s.GetProductByIdAsync(7))
            .ReturnsAsync(product);

        _mockService
            .Setup(s => s.DeleteProductByIdAsync(7))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProduct(7);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockService.Verify(s => s.DeleteProductByIdAsync(7), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        // nothing

        // Act
        var result = await _controller.DeleteProduct(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
