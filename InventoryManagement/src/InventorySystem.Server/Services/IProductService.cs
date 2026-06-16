using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public interface IProductService
{
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> GetProductByIdAsync(int id);
    Task<List<Product>> GetAllProductsAsync();
    Task<ProductStatsDto> GetProductStatsAsync();
    Task UpdateProductAsync(Product product);
    Task DeleteProductByIdAsync(int id);
}
