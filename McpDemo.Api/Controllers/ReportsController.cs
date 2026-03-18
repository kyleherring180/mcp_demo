using McpDemo.Api.Data;
using McpDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace McpDemo.Api.Controllers;

// ⚠ This file contains intentional SonarQube issues for demo purposes.
[ApiController]
[Route("api/[controller]")]
public class ReportsController(AppDbContext db, ProductReportService reportService) : ControllerBase
{
    // S1172: Unused method parameter (Code Smell)
    [HttpGet("inventory-value")]
    public async Task<ActionResult<decimal>> GetInventoryValue(string unusedFilter)
    {
        var total = await reportService.GetTotalInventoryValueAsync();
        return Ok(total);
    }

    // S4457: Non-async entry but calls async without awaiting — sync-over-async (Bug)
    [HttpGet("low-stock")]
    public ActionResult GetLowStock()
    {
        var products = reportService.GetLowStockProductsAsync().Result;
        return Ok(products);
    }

    // S2737: Catch block re-throws same exception adding no value (Code Smell)
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary()
    {
        try
        {
            var totalProducts = await db.Products.CountAsync();
            var activeProducts = await db.Products.CountAsync(p => p.IsActive);
            var totalCategories = await db.Categories.CountAsync();
            var outOfStock = await db.Products.CountAsync(p => p.Stock == 0);

            return Ok(new
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                TotalCategories = totalCategories,
                OutOfStockProducts = outOfStock
            });
        }
        catch (Exception ex)
        {
            // S2737: Catch re-throws without adding context (Code Smell)
            throw ex;
        }
    }

    // S2971: IQueryable.Count() called after ToList() — inefficient (Code Smell)
    [HttpGet("category-counts")]
    public async Task<ActionResult<object>> GetCategoryCounts()
    {
        var allProducts = await db.Products.Include(p => p.Category).ToListAsync();

        // S2971: Count on in-memory list instead of pushing predicate to SQL
        var electronicCount = allProducts.Where(p => p.Category.Name == "Electronics").Count();
        var clothingCount = allProducts.Where(p => p.Category.Name == "Clothing").Count();
        var bookCount = allProducts.Where(p => p.Category.Name == "Books").Count();

        return Ok(new { Electronics = electronicCount, Clothing = clothingCount, Books = bookCount });
    }
}
