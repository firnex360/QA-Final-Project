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

        var pagedResponse = new PagedResponse<Product>
        {
            Items = products,
            TotalCount = 2,
            TotalPages = 1,
            CurrentPage = 1
        };

        var parameters = new ProductQueryParameters();

        _mockService
            .Setup(s => s.GetProductsFilterAsync(It.IsAny<ProductQueryParameters>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAllProducts(parameters);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<PagedResponse<Product>>(okResult.Value);
        Assert.Equal(2, returnedResponse.Items.Count);
    }

    [Fact]
    public async Task GetAllProducts_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        var pagedResponse = new PagedResponse<Product>
        {
            Items = [],
            TotalCount = 0,
            TotalPages = 0,
            CurrentPage = 1
        };

        var parameters = new ProductQueryParameters();

        _mockService
            .Setup(s => s.GetProductsFilterAsync(It.IsAny<ProductQueryParameters>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAllProducts(parameters);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<PagedResponse<Product>>(okResult.Value);
        Assert.Empty(returnedResponse.Items);
        Assert.Equal(0, returnedResponse.TotalCount);
    }

    [Fact]
    public async Task GetAllProducts_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var parameters = new ProductQueryParameters();

        _mockService
            .Setup(s => s.GetProductsFilterAsync(It.IsAny<ProductQueryParameters>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetAllProducts(parameters);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving products. \n\nException Message: Database connection failed", objectResult.Value);
    }

    [Fact]
    public async Task GetAllProducts_WithSearchTerm_PassesParametersToService()
    {
        // Arrange
        var parameters = new ProductQueryParameters { SearchTerm = "apple" };
        ProductQueryParameters? capturedParams = null;

        _mockService
            .Setup(s => s.GetProductsFilterAsync(It.IsAny<ProductQueryParameters>()))
            .Callback<ProductQueryParameters>(p => capturedParams = p)
            .ReturnsAsync(new PagedResponse<Product>());

        // Act
        await _controller.GetAllProducts(parameters);

        // Assert
        Assert.NotNull(capturedParams);
        Assert.Equal("apple", capturedParams.SearchTerm);
    }

    [Fact]
    public async Task GetAllProducts_WithCategory_PassesParametersToService()
    {
        // Arrange
        var parameters = new ProductQueryParameters { Category = "Electronics" };
        ProductQueryParameters? capturedParams = null;

        _mockService
            .Setup(s => s.GetProductsFilterAsync(It.IsAny<ProductQueryParameters>()))
            .Callback<ProductQueryParameters>(p => capturedParams = p)
            .ReturnsAsync(new PagedResponse<Product>());

        // Act
        await _controller.GetAllProducts(parameters);

        // Assert
        Assert.NotNull(capturedParams);
        Assert.Equal("Electronics", capturedParams.Category);
    }

    [Fact]
    public async Task GetAllProducts_WithPagination_PassesParametersToService()
    {
        // Arrange
        var parameters = new ProductQueryParameters { PageNumber = 3, PageSize = 15 };
        ProductQueryParameters? capturedParams = null;

        _mockService
            .Setup(s => s.GetProductsFilterAsync(It.IsAny<ProductQueryParameters>()))
            .Callback<ProductQueryParameters>(p => capturedParams = p)
            .ReturnsAsync(new PagedResponse<Product>());

        // Act
        await _controller.GetAllProducts(parameters);

        // Assert
        Assert.NotNull(capturedParams);
        Assert.Equal(3, capturedParams.PageNumber);
        Assert.Equal(15, capturedParams.PageSize);
    }
}
