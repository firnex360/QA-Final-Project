using System.Net;
using System.Net.Http.Json;
using Integration.Fixtures;
using InventorySystem.Shared.Models;

namespace Integration.Tests;

/// <summary>
/// Integration tests for the Product API (api/product).
/// Each test class instance gets a fresh "InventoryApiFactory" 
/// with its own testcontainer, so tests are fully isolated.
/// </summary>
public class ProductApiTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;

    public ProductApiTests(InventoryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // initroduce a helper method to create valid products 

    private static Product MakeValidProduct(string? nameSuffix = null) => new()
    {
        Name = $"Test Product {nameSuffix ?? Guid.NewGuid().ToString()[..6]}",
        CodeSKU = $"SKU-{Guid.NewGuid().ToString()[..8]}",
        Description = "A product created by integration tests.",
        Category = "Testing",
        Price = 29.99m,
        Quantity = 100,
        MinimumStockLevel = 10,
        IsActive = true
    };

    private async Task<int> CreateProductAndGetId(Product? product = null)
    {
        product ??= MakeValidProduct();
        var response = await _client.PostAsJsonAsync("/api/product", product);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        return body!.ProductId;
    }

    // tests for create
    
    [Fact]
    public async Task CreateProduct_ReturnsSuccess()
    {
        // Arrange
        var product = MakeValidProduct("Create");

        // Act
        var response = await _client.PostAsJsonAsync("/api/product", product, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateProductResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains("successfully", body.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(body.ProductId > 0);
        Assert.Equal(product.Name, body.ProductName);
    }

    [Fact]
    public async Task CreateProduct_MissingName_ReturnsBadRequest()
    {
        // Arrange
        var product = MakeValidProduct();
        product.Name = "";  

        // Act
        var response = await _client.PostAsJsonAsync("/api/product", product, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_NegativePrice_ReturnsBadRequest()
    {
        // Arrange
        var product = MakeValidProduct();
        product.Price = -5m;

        // Act
        var response = await _client.PostAsJsonAsync("/api/product", product, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // test for get all

    [Fact]
    public async Task GetAllProducts_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/product", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsProducts_AfterCreate()
    {
        // Arrange
        var product = MakeValidProduct("GetAll");
        await _client.PostAsJsonAsync("/api/product", product, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await _client.GetAsync("/api/product", TestContext.Current.CancellationToken);
        var products = await response.Content.ReadFromJsonAsync<PagedResponse<Product>>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(products);
        Assert.Contains(products.Items, p => p.Name == product.Name);
    }

    // test for get by id

    [Fact]
    public async Task GetProductById_ReturnsProduct()
    {
        // Arrange
        var product = MakeValidProduct("GetById");
        var id = await CreateProductAndGetId(product);

        // Act
        var response = await _client.GetAsync($"/api/product/{id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<Product>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(returned);
        Assert.Equal(product.Name, returned.Name);
        Assert.Equal(product.CodeSKU, returned.CodeSKU);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenIdDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/product/99999", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // test for update 

    [Fact]
    public async Task UpdateProduct_ReturnsUpdatedProduct()
    {
        // Arrange
        var id = await CreateProductAndGetId();

        var updatedData = new Product
        {
            Name = "Updated Product Name",
            CodeSKU = "SKU-UPDATED",
            Description = "Updated description.",
            Category = "Updated Category",
            Price = 59.99m,
            Quantity = 200,
            MinimumStockLevel = 20,
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/product/{id}", updatedData, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<Product>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(returned);
        Assert.Equal("Updated Product Name", returned.Name);
        Assert.Equal(59.99m, returned.Price);
        Assert.False(returned.IsActive);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsNotFound_WhenIdDoesNotExist()
    {
        var updatedData = MakeValidProduct("UpdateNotFound");

        // Act
        var response = await _client.PutAsJsonAsync("/api/product/99999", updatedData, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // test for delete 

    [Fact]
    public async Task DeleteProduct_ReturnsSuccess()
    {
        // Arrange
        var id = await CreateProductAndGetId();

        // Act
        var response = await _client.DeleteAsync($"/api/product/{id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify it's actually gone
        var getResponse = await _client.GetAsync($"/api/product/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_WhenIdDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/product/99999", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // tmp dto

    private record CreateProductResponse(string Message, int ProductId, string ProductName);
}
