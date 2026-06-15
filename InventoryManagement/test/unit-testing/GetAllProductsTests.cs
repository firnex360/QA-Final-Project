using InventorySystem.Server.Controllers;
using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_testing;

/// <summary>
/// Tests for GET api/product — retrieve all products.
/// </summary>
public class GetAllProductsTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly ProductController _controller;

    public GetAllProductsTests()
    {
        _mockService = new Mock<IProductService>();
        _controller = new ProductController(_mockService.Object);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOkWithListOfProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Product A", CodeSKU = "A-001", Description = "Desc A", Category = "Cat A", Price = 10m, Quantity = 5, MinimumStockLevel = 1, IsActive = true },
            new() { Id = 2, Name = "Product B", CodeSKU = "B-002", Description = "Desc B", Category = "Cat B", Price = 20m, Quantity = 10, MinimumStockLevel = 2, IsActive = true }
        };

        _mockService
            .Setup(s => s.GetAllProductsAsync())
            .ReturnsAsync(products);

        // Act
        var result = await _controller.GetAllProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProducts = Assert.IsType<List<Product>>(okResult.Value);
        Assert.Equal(2, returnedProducts.Count);
    }

    [Fact]
    public async Task GetAllProducts_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAllProductsAsync())
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _controller.GetAllProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProducts = Assert.IsType<List<Product>>(okResult.Value);
        Assert.Empty(returnedProducts);
    }
}
