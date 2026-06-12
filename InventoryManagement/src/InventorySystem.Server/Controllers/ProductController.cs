using InventorySystem.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    [HttpPost("create")]
    public async Task<IActionResult> TestCreate()
    {
        var product = await _productService.CreateProductAsync();

        return Ok(new
        {
            Message = "Product created successfully!",
            ProductId = product.Id,
            ProductName = product.Name
        });
    }
}
