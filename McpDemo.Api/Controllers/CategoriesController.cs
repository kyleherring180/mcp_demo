using McpDemo.Api.Data;
using McpDemo.Api.DTOs;
using McpDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace McpDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db, ILogger<CategoriesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        logger.LogInformation("Fetching all categories");

        var categories = await db.Categories
            .Include(c => c.Products)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.Products.Count, c.CreatedAt))
            .ToListAsync();

        logger.LogInformation("Returned {CategoryCount} categories", categories.Count);
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        logger.LogInformation("Fetching category {CategoryId}", id);

        var category = await db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            logger.LogWarning("Category {CategoryId} not found", id);
            return NotFound();
        }

        logger.LogInformation("Returned category {CategoryId} ({CategoryName}) with {ProductCount} products", category.Id, category.Name, category.Products.Count);
        return Ok(new CategoryDto(category.Id, category.Name, category.Description, category.Products.Count, category.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request)
    {
        logger.LogInformation("Creating category {CategoryName}", request.Name);

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        logger.LogInformation("Created category {CategoryId} ({CategoryName})", category.Id, category.Name);
        var dto = new CategoryDto(category.Id, category.Name, category.Description, 0, category.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request)
    {
        logger.LogInformation("Updating category {CategoryId}", id);

        var category = await db.Categories.FindAsync(id);
        if (category is null)
        {
            logger.LogWarning("Update failed — category {CategoryId} not found", id);
            return NotFound();
        }

        category.Name = request.Name;
        category.Description = request.Description;

        await db.SaveChangesAsync();
        logger.LogInformation("Updated category {CategoryId} ({CategoryName})", category.Id, category.Name);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        logger.LogInformation("Deleting category {CategoryId}", id);

        var category = await db.Categories.FindAsync(id);
        if (category is null)
        {
            logger.LogWarning("Delete failed — category {CategoryId} not found", id);
            return NotFound();
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        logger.LogInformation("Deleted category {CategoryId} ({CategoryName})", id, category.Name);
        return NoContent();
    }
}
