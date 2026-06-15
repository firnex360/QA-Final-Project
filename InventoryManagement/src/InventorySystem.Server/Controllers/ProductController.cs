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
    [HttpPost("testcreate")]
    public async Task<IActionResult> TestCreate()
    {
        var product = await _productService.CreateProductAsync(new Product
        {
            Name = "Test Product",
            CodeSKU = "TEST-001",
            Description = "Hardcoded product for endpoint testing",
            Category = "Testing",
            Price = 10m,
            Quantity = 5,
            MinimumStockLevel = 1,
            IsActive = true
        });

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

        var created = await _productService.CreateProductAsync(product);

        return Ok(new
        {
            Message = "Product created successfully!",
            ProductId = created.Id,
            ProductName = created.Name
        });
    }

    // GET api/product
    [HttpGet]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    // GET api/product/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    // PUT api/product/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
    {
        if (product is null)
            return BadRequest("Product body is required.");

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
