using InventorySystem.Server.Controllers;
using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_testing;

/// <summary>
/// Tests for POST api/product — create a product from a JSON body.
/// </summary>
public class CreateProductTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly ProductController _controller;

    public CreateProductTests()
    {
        _mockService = new Mock<IProductService>();
        _controller = new ProductController(_mockService.Object);
    }

    [Fact]
    public async Task CreateProduct_ValidProduct_ReturnsOkWithProductInfo()
    {
        // Arrange
        var newProduct = new Product
        {
            Name = "Keyboard",
            CodeSKU = "KB-100",
            Description = "Mechanical keyboard",
            Category = "Peripherals",
            Price = 79.99m,
            Quantity = 50,
            MinimumStockLevel = 5,
            IsActive = true
        };

        var createdProduct = new Product
        {
            Name = "Keyboard",
            CodeSKU = "KB-100",
            Description = "Mechanical keyboard",
            Category = "Peripherals",
            Price = 79.99m,
            Quantity = 50,
            MinimumStockLevel = 5,
            IsActive = true
        };

        _mockService
            .Setup(s => s.CreateProductAsync(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _controller.CreateProduct(newProduct);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockService.Verify(s => s.CreateProductAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_MissingName_ReturnsBadRequest()
    {
        // Arrange — product with empty Name
        var invalidProduct = new Product
        {
            Name = "",
            CodeSKU = "KB-100",
            Description = "Mechanical keyboard",
            Category = "Peripherals",
            Price = 79.99m,
            Quantity = 50,
            MinimumStockLevel = 5,
            IsActive = true
        };

        // Act
        var result = await _controller.CreateProduct(invalidProduct);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Name is required.", badRequest.Value);
        _mockService.Verify(s => s.CreateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProduct_Null_ReturnsBadRequest()
    {
        // Arrange
        Product? invalidProduct = null;

        // Act
        var result = await _controller.CreateProduct(invalidProduct!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Product body is required.", badRequest.Value);
        _mockService.Verify(s => s.CreateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProduct_IdAssign_ReturnsBadRequest()
    {
        // Arrange
        var createdProduct = new Product
        {
            Id = 10,
            Name = "Keyboard",
            CodeSKU = "KB-100",
            Description = "Mechanical keyboard",
            Category = "Peripherals",
            Price = 79.99m,
            Quantity = 50,
            MinimumStockLevel = 5,
            IsActive = true
        };

        // Act
        var result = await _controller.CreateProduct(createdProduct);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Can't assigned values to ID.", badRequest.Value);
        _mockService.Verify(s => s.CreateProductAsync(It.IsAny<Product>()), Times.Never);
    }
}
