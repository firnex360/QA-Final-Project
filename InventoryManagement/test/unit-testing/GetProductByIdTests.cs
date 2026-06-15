using InventorySystem.Server.Controllers;
using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_testing;

/// <summary>
/// Tests for GET api/product/{id} — retrieve a single product by ID.
/// </summary>
public class GetProductByIdTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly ProductController _controller;

    public GetProductByIdTests()
    {
        _mockService = new Mock<IProductService>();
        _controller = new ProductController(_mockService.Object);
    }

    [Fact]
    public async Task GetProductById_ExistingId_ReturnsOkWithProduct()
    {
        // Arrange
        var product = new Product
        {
            Id = 5,
            Name = "Monitor",
            CodeSKU = "MON-500",
            Description = "27-inch monitor",
            Category = "Displays",
            Price = 299.99m,
            Quantity = 12,
            MinimumStockLevel = 3,
            IsActive = true
        };

        _mockService
            .Setup(s => s.GetProductByIdAsync(5))
            .ReturnsAsync(product);

        // Act
        var result = await _controller.GetProductById(5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProduct = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(5, returnedProduct.Id);
        Assert.Equal("Monitor", returnedProduct.Name);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetProductByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _controller.GetProductById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
