using InventorySystem.Server.Controllers;
using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_testing;

/// <summary>
/// Tests for PUT api/product/{id} — update an existing product.
/// </summary>
public class UpdateProductTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly ProductController _controller;

    public UpdateProductTests()
    {
        _mockService = new Mock<IProductService>();
        _controller = new ProductController(_mockService.Object);
    }

    [Fact]
    public async Task UpdateProduct_ValidProduct_ReturnsOkWithUpdatedProduct()
    {
        // Arrange
        var existingProduct = new Product
        {
            Id = 3,
            Name = "Old Mouse",
            CodeSKU = "MS-300",
            Description = "Old description",
            Category = "Peripherals",
            Price = 25m,
            Quantity = 30,
            MinimumStockLevel = 5,
            IsActive = true
        };

        var updatedData = new Product
        {
            Name = "New Mouse Pro",
            CodeSKU = "MS-300-PRO",
            Description = "Updated ergonomic mouse",
            Category = "Peripherals",
            Price = 45m,
            Quantity = 100,
            MinimumStockLevel = 10,
            IsActive = true
        };

        _mockService
            .Setup(s => s.GetProductByIdAsync(3))
            .ReturnsAsync(existingProduct);

        _mockService
            .Setup(s => s.UpdateProductAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateProduct(3, updatedData);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProduct = Assert.IsType<Product>(okResult.Value);
        Assert.Equal("New Mouse Pro", returnedProduct.Name);
        Assert.Equal(45m, returnedProduct.Price);
        _mockService.Verify(s => s.UpdateProductAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var updatedData = new Product
        {
            Name = "Ghost Product",
            CodeSKU = "GHOST-001",
            Description = "Does not exist",
            Category = "N/A",
            Price = 1m,
            Quantity = 0,
            MinimumStockLevel = 0,
            IsActive = false
        };

        _mockService
            .Setup(s => s.GetProductByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _controller.UpdateProduct(999, updatedData);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockService.Verify(s => s.UpdateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProduct_UpdatingId_ReturnsBadRequest()
    {
        // Arrange
        var data = new Product
        {
            Id = 5,
            Name = "Ghost Product",
            CodeSKU = "GHOST-001",
            Description = "Does not exist",
            Category = "N/A",
            Price = 1m,
            Quantity = 0,
            MinimumStockLevel = 0,
            IsActive = false
        };

        var updatedData = new Product
        {
            Id = 10, 
            Name = "New Mouse Pro",
            CodeSKU = "MS-300-PRO",
            Description = "Updated ergonomic mouse",
            Category = "Peripherals",
            Price = 45m,
            Quantity = 100,
            MinimumStockLevel = 10,
            IsActive = true
        };

        _mockService
            .Setup(s => s.GetProductByIdAsync(5))
            .ReturnsAsync(data);

        // Act
        var result = await _controller.UpdateProduct(5, updatedData);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Can't update value ID.", badRequest.Value);
        _mockService.Verify(s => s.UpdateProductAsync(It.IsAny<Product>()), Times.Never);
    }
}
