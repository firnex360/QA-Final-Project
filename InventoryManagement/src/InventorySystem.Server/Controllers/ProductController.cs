using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    // POST api/product/create  (hardcoded test product)
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

    // POST api/product: accepts a Product JSON body from the client (this is the one to use)
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _productService.CreateProductAsync(product);

        return Ok(new
        {
            Message = "Product created successfully!",
            ProductId = created.Id,
            ProductName = created.Name
        });
    }
}
