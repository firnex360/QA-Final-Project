using InventorySystem.Shared.Models;

namespace InventorySystem.Server.Services;

public interface IProductService
{
    Task<Product> CreateProductAsync();
}
