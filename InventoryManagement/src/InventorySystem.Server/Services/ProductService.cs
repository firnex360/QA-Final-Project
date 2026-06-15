using InventorySystem.Server.Repositories;
using InventorySystem.Shared.DTOs;
using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public class ProductService(IProductRepository repository) : IProductService
{
    private readonly IProductRepository _repository = repository;

    public async Task<Product> CreateProductAsync(Product product) =>
        await _repository.CreateAsync(product);

    public async Task<Product> CreateProductAsync(CreateProductDto dto)
    {
        if (await _repository.ExistsAsync(dto.Name!))
            throw new InvalidOperationException($"A product named '{dto.Name}' already exists.");

        var product = new Product
        {
            Name = dto.Name,
            CodeSKU = dto.CodeSKU,
            Description = dto.Description,
            Category = dto.Category,
            Price = dto.Price,
            Quantity = dto.Quantity,
            MinimumStockLevel = dto.MinimumStockLevel,
            IsActive = dto.IsActive
        };
        return await _repository.CreateAsync(product);
    }

    public async Task<Product?> GetProductByIdAsync(int id) =>
        await _repository.GetByIdAsync(id);

    public async Task<List<Product>> GetAllProductsAsync() =>
        await _repository.GetAllAsync();

    public async Task<List<Product>> GetLowStockAsync(int threshold)
    {
        var all = await _repository.GetAllAsync();
        return all
            .Where(p => p.Quantity < threshold)
            .OrderBy(p => p.Quantity)
            .ToList();
    }

    public async Task UpdateProductAsync(Product product) =>
        await _repository.UpdateAsync(product);

    public async Task DeleteProductByIdAsync(int id) =>
        await _repository.DeleteAsync(id);
}
