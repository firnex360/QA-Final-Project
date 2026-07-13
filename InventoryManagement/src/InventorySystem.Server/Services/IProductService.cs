using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public interface IProductService
{
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> GetProductByIdAsync(int id);
    Task<List<Product>> GetAllProductsAsync();
    Task<PagedResponse<Product>> GetProductsFilterAsync(ProductQueryParameters parameters);
    Task<ProductStatsDto> GetProductStatsAsync();
    Task UpdateProductAsync(Product product);
    Task<Product?> AdjustStockAsync(int id, int delta);
    Task DeleteProductByIdAsync(int id);
}
