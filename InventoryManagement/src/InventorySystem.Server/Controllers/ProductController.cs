using InventorySystem.Server.Services;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace InventorySystem.Server.Controllers;

// #1.0-product-api
// REST API for "Gestión de Productos". Base route: /api/product
//
//   POST   /api/product          → create           (#1.1-create-api)
//   GET    /api/product          → list + paging    (#1.4-list-api)
//   GET    /api/product/stats    → dashboard totals (see Home dashboard)
//   GET    /api/product/{id}     → single product   (#1.4-get-by-id-api)
//   PUT    /api/product/{id}     → update           (#1.2-edit-api)
//   PATCH  /api/product/{id}/stock → stock in/out   (stock module)
//   DELETE /api/product/{id}     → delete           (#1.3-delete-api)
//
// Authorization: every action carries [Authorize] (valid JWT required); the actual
// permission check happens in PolicyEnforcementMiddleware, which derives the scope
// from the HTTP verb — GET→view, DELETE→delete, POST/PUT/PATCH→manage (#1.6-product-permissions).
[Route("api/[controller]")]
[ApiController]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    // #1.1-create-api
    // POST /api/product — creates a product from a JSON body.
    // Rejects (400) when: Id is supplied, Name/SKU/Description/Category blank,
    // Price <= 0, Quantity < 0, or MinimumStockLevel < 0.
    // Returns 200 with { Message, ProductId, ProductName }; 500 on unexpected failure.
    // Requires the "manage" scope on the Products resource.
    [HttpPost]
    [Authorize]
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

    // #1.4-list-api
    // GET /api/product — paginated, searchable, filterable, sortable product list.
    // Query string binds to ProductQueryParameters (#1.0-query-params), e.g.
    //   /api/product?pageNumber=1&pageSize=8&searchTerm=cable&category=Electronics
    //                &sortBy=price&sortDescending=true&lowStockOnly=true
    // All the heavy lifting is in the service (#1.4-list-service); this action only
    // adapts it to HTTP. Returns PagedResponse<Product> (#1.0-paged-response).
    [HttpGet]
    [Authorize]
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

    // #4.1-dashboard-stats-api
    // GET /api/product/stats — every figure the dashboard needs, in one call.
    // Also reused by the products page to populate the category dropdown and the live
    // low-stock count (#1.4.2-filters-ui). Computed in #4.1-dashboard-stats.
    // Guarded by its own Keycloak resource (ProductStats), so read-only roles can see
    // reports without being granted access to the product list itself.
    [HttpGet("stats")]
    [Authorize]
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

    // #1.4-get-by-id-api
    // GET /api/product/{id} — single product, or 404 when it does not exist.
    // Used by the Edit page to prefill its form (#1.2-edit-ui).
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    // #1.2-edit-api
    // PUT /api/product/{id} — full update of an existing product.
    // Same validation rules as create, plus: the body Id (when non-zero) must match
    // the URL id, otherwise 400. Returns 404 when the product does not exist.
    // Loads the tracked entity first and copies each field onto it, so EF Core emits
    // a real UPDATE and the audit trail records the before/after values.
    // Requires the "manage" scope.
    [HttpPut("{id:int}")]
    [Authorize]
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

    // #2.1-adjust-api
    // PATCH /api/product/{id}/stock?delta=N — stock entry/exit ("entrada y salida").
    // delta > 0 adds stock, delta < 0 removes it; delta == 0 is rejected with 400.
    // 400 also when the movement would push the quantity below zero; 404 when the
    // product does not exist. Requires the ProductStock:manage scope.
    [HttpPatch("{id:int}/stock")]
    [Authorize]
    public async Task<IActionResult> AdjustStock(int id, [FromQuery] int delta)
    {
        if (delta == 0)
            return BadRequest("Delta cannot be zero.");

        try
        {
            var updated = await _productService.AdjustStockAsync(id, delta);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // #1.3-delete-api
    // DELETE /api/product/{id} — hard delete. Returns 404 when the product does not
    // exist, otherwise 200 with a confirmation message.
    // Requires the "delete" scope, which is granted separately from "manage" — a role
    // can be allowed to edit products without being allowed to remove them.
    [HttpDelete("{id:int}")]
    [Authorize]
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
