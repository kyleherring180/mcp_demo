using McpDemo.Api.Data;
using McpDemo.Api.DTOs;
using McpDemo.Api.Models;
using McpDemo.Api.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace McpDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db, ProductMetrics metrics, ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        logger.LogInformation("Fetching all products");

        var products = await db.Products
            .Include(p => p.Category)
            .Select(p => ToDto(p))
            .ToListAsync();

        logger.LogInformation("Returned {ProductCount} products", products.Count);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        logger.LogInformation("Fetching product {ProductId}", id);

        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found", id);
            return NotFound();
        }

        metrics.RecordProductView(product.Id, product.Category.Name);
        logger.LogInformation("Returned product {ProductId} ({ProductName})", product.Id, product.Name);
        return Ok(ToDto(product));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Search([FromQuery] ProductSearchRequest request)
    {
        logger.LogInformation(
            "Searching products — query: {Query}, categoryId: {CategoryId}, price: {MinPrice}-{MaxPrice}, inStockOnly: {InStockOnly}",
            request.Query, request.CategoryId, request.MinPrice, request.MaxPrice, request.InStockOnly);

        var query = db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
            query = query.Where(p =>
                p.Name.Contains(request.Query) ||
                p.Description!.Contains(request.Query) ||
                p.Sku.Contains(request.Query));

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId);

        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price >= request.MinPrice);

        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= request.MaxPrice);

        if (request.InStockOnly == true)
            query = query.Where(p => p.Stock > 0);

        var results = await query.Select(p => ToDto(p)).ToListAsync();

        metrics.RecordSearch(request.CategoryId, !string.IsNullOrWhiteSpace(request.Query), results.Count);
        logger.LogInformation("Search returned {ResultCount} products", results.Count);

        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
    {
        logger.LogInformation("Creating product {ProductName} (SKU: {Sku})", request.Name, request.Sku);

        var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
        {
            logger.LogWarning("Create failed — category {CategoryId} not found", request.CategoryId);
            return BadRequest($"Category {request.CategoryId} does not exist.");
        }

        var skuTaken = await db.Products.AnyAsync(p => p.Sku == request.Sku);
        if (skuTaken)
        {
            logger.LogWarning("Create failed — SKU {Sku} already in use", request.Sku);
            return Conflict($"SKU '{request.Sku}' is already in use.");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        await db.Entry(product).Reference(p => p.Category).LoadAsync();

        metrics.RecordProductCreated(product.Category.Name);
        logger.LogInformation("Created product {ProductId} ({ProductName})", product.Id, product.Name);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        logger.LogInformation("Updating product {ProductId}", id);

        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            logger.LogWarning("Update failed — product {ProductId} not found", id);
            return NotFound();
        }

        var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
        {
            logger.LogWarning("Update failed — category {CategoryId} not found", request.CategoryId);
            return BadRequest($"Category {request.CategoryId} does not exist.");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        metrics.RecordProductUpdated();
        logger.LogInformation("Updated product {ProductId} ({ProductName})", product.Id, product.Name);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        logger.LogInformation("Deleting product {ProductId}", id);

        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            logger.LogWarning("Delete failed — product {ProductId} not found", id);
            return NotFound();
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        metrics.RecordProductDeleted();
        logger.LogInformation("Deleted product {ProductId} ({ProductName})", id, product.Name);

        return NoContent();
    }

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Sku, p.Price, p.Stock, p.IsActive,
            p.CategoryId, p.Category.Name, p.CreatedAt, p.UpdatedAt);
}
