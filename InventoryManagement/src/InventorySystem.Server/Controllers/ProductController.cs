using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace InventorySystem.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    // POST api/product: accepts a Product JSON body from the client (this is the one to use)
    [HttpPost]
    [Authorize(Policy = "CanCreate")]  
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        if (product is null)
            return BadRequest("Product body is required.");

        if (product.Id > 0)
            return BadRequest("Can't assigned values to ID.");
        if (string.IsNullOrWhiteSpace(product.Name))
            return BadRequest("Name is required.");
        if (string.IsNullOrWhiteSpace(product.CodeSKU))
            return BadRequest("SKU is required.");
        if (string.IsNullOrWhiteSpace(product.Description))
            return BadRequest("Description is required.");
        if (string.IsNullOrWhiteSpace(product.Category))
            return BadRequest("Category is required.");
        if (product.Price <= 0)
            return BadRequest("Price must be greater than 0.");
        if (product.Quantity < 0)
            return BadRequest("Quantity cannot be negative.");
        if (product.MinimumStockLevel < 0)
            return BadRequest("Minimum stock level cannot be negative.");

        try
        {
            var created = await _productService.CreateProductAsync(product);

            return Ok(new
            {
                Message = "Product created successfully!",
                ProductId = created.Id,
                ProductName = created.Name
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while creating products. \n\nException Message: {ex.Message}");
        }
    }

    // GET api/product
    // Accepts filters from the body 
    [HttpGet]
    //[Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryParameters parameters)
    {
        try
        {
            var result = await _productService.GetProductsFilterAsync(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving products. \n\nException Message: {ex.Message}");
        }
    }

    // GET api/product/stats  — aggregated figures for the home dashboard
    [HttpGet("stats")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _productService.GetProductStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving products. \n\nException Message: {ex.Message}");
        }
    }

    // GET api/product/{id}
    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    // PUT api/product/{id}
    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanUpdate")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
    {
        if (product is null)
            return BadRequest("Product body is required.");

        if (product.Id != 0 && product.Id != id)
            return BadRequest("Body ID does not match the URL ID.");
        if (string.IsNullOrWhiteSpace(product.Name))
            return BadRequest("Name is required.");
        if (string.IsNullOrWhiteSpace(product.CodeSKU))
            return BadRequest("SKU is required.");
        if (string.IsNullOrWhiteSpace(product.Description))
            return BadRequest("Description is required.");
        if (string.IsNullOrWhiteSpace(product.Category))
            return BadRequest("Category is required.");
        if (product.Price <= 0)
            return BadRequest("Price must be greater than 0.");
        if (product.Quantity < 0)
            return BadRequest("Quantity cannot be negative.");
        if (product.MinimumStockLevel < 0)
            return BadRequest("Minimum stock level cannot be negative.");

        var existingProduct = await _productService.GetProductByIdAsync(id);

        if (existingProduct == null)
            return NotFound();

        existingProduct.Name = product.Name;
        existingProduct.CodeSKU = product.CodeSKU;
        existingProduct.Description = product.Description;
        existingProduct.Category = product.Category;
        existingProduct.Price = product.Price;
        existingProduct.Quantity = product.Quantity;
        existingProduct.MinimumStockLevel = product.MinimumStockLevel;
        existingProduct.IsActive = product.IsActive;

        await _productService.UpdateProductAsync(existingProduct);

        return Ok(existingProduct);
    }

    // DELETE api/product/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanDelete")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        await _productService.DeleteProductByIdAsync(id);

        return Ok(new 
        { 
            Message = "Product deleted successfully with id: " + product.Id
        });
    }
}
