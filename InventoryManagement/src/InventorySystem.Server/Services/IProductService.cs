using InventorySystem.Shared.DTOs;
using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public interface IProductService
{
    Task<Product> CreateProductAsync(Product product);
    Task<Product> CreateProductAsync(CreateProductDto dto);
    Task<Product?> GetProductByIdAsync(int id);
    Task<List<Product>> GetAllProductsAsync();
    Task<List<Product>> GetLowStockAsync(int threshold);
    Task UpdateProductAsync(Product product);
    Task DeleteProductByIdAsync(int id);
}
