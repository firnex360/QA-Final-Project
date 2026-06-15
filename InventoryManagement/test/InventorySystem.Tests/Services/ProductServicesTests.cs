// InventorySystem.Tests/Services/ProductServiceTests.cs
using InventorySystem.Server.Repositories;
using InventorySystem.Server.Services;
using InventorySystem.Shared.DTOs;
using InventorySystem.Shared.Models;
using Moq;
using Xunit;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repoMock;
    private readonly ProductService _sut; // system under test

    public ProductServiceTests()
    {
        _repoMock = new Mock<IProductRepository>();
        _sut = new ProductService(_repoMock.Object);
    }

    [Fact]
    public async Task GetLowStock_ReturnsSortedProducts_BelowThreshold()
    {
        // Arrange
        var fakeProducts = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Quantity = 3 },
            new() { Id = 2, Name = "Gadget", Quantity = 15 },
            new() { Id = 3, Name = "Doohickey", Quantity = 1 },
        };

        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(fakeProducts);

        // Act
        var result = await _sut.GetLowStockAsync(threshold: 5);

        // Assert
        Assert.Equal(2, result.Count);                      // only Quantity < 5
        Assert.Equal("Doohickey", result.First().Name);     // sorted ascending
        _repoMock.Verify(r => r.GetAllAsync(), Times.Once); // called exactly once
    }

    [Fact]
    public async Task CreateProduct_ThrowsException_WhenNameIsDuplicate()
    {
        _repoMock
            .Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateProductAsync(new CreateProductDto { Name = "Widget" })
        );
    }
}
