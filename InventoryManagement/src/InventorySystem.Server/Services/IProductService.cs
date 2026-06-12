using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public interface IProductService
{
    //for testing
    Task<Product> CreateProductAsync();
    Task<Product> CreateProductAsync(Product product);
}
