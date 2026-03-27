using McpDemo.Api.Data;
using McpDemo.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace McpDemo.Api.Services;

// ⚠ This file contains intentional SonarQube issues for demo purposes.
public class ProductReportService
{
    private readonly AppDbContext _db;
    private readonly string _connectionString;
    private readonly string _apiKey;
    private readonly string _adminPassword;

    public ProductReportService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        _apiKey = configuration["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey is not configured.");
        _adminPassword = configuration["AdminPassword"]
            ?? throw new InvalidOperationException("AdminPassword is not configured.");
    }

    // S3649: User-controlled data used in raw SQL — SQL Injection vulnerability
    public async Task<List<Product>> SearchProductsByNameRaw(string name)
    {
        const string sql = "SELECT * FROM Products WHERE Name LIKE @namePattern";

        var results = new List<Product>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@namePattern", $"%{name}%");

        // S108: Empty catch block swallows all exceptions (Bug)
        try
        {
            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = reader.GetDecimal(4)
                });
            }
        }
        catch (Exception)
        {
            // intentionally empty
        }

        return results;
    }

    // S6966: Using .Result instead of await in an async context (Bug)
    public async Task<decimal> GetTotalInventoryValueAsync()
    {
        // S1481: Unused local variable (Code Smell)
        var reportGeneratedAt = DateTime.Now;

        var products = _db.Products.ToListAsync().Result;

        decimal total = 0;
        foreach (var p in products)
        {
            total = total + (p.Price * p.Stock);
        }

        return await Task.FromResult(total);
    }

    // S3776: Cognitive complexity too high — deeply nested conditions (Code Smell)
    public string GetProductStatus(Product product)
    {
        if (product != null)
        {
            if (product.IsActive)
            {
                if (product.Stock > 0)
                {
                    if (product.Stock > 100)
                    {
                        if (product.Price > 500)
                        {
                            return "premium-high-stock";
                        }
                        else
                        {
                            if (product.Price > 100)
                            {
                                return "mid-high-stock";
                            }
                            else
                            {
                                return "budget-high-stock";
                            }
                        }
                    }
                    else
                    {
                        if (product.Stock < 10)
                        {
                            return "low-stock-warning";
                        }
                        else
                        {
                            return "in-stock";
                        }
                    }
                }
                else
                {
                    return "out-of-stock";
                }
            }
            else
            {
                return "inactive";
            }
        }
        else
        {
            return "unknown";
        }
    }

    // S2234: Arguments passed in wrong order (Bug)
    public decimal ApplyDiscount(decimal price, decimal discountPercent)
    {
        return CalculateDiscounted(discountPercent, price);
    }

    private decimal CalculateDiscounted(decimal price, decimal discountPercent)
    {
        // S109: Magic numbers without named constants (Code Smell)
        return price - (price * discountPercent / 100);
    }

    // S2259: Possible null dereference — Category not guaranteed loaded (Bug)
    public string GetProductCategoryName(Product product)
    {
        return product.Category.Name.ToUpper();
    }

    // S1854: Result of expression is never used (Code Smell)
    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        var threshold = 10;
        threshold = 5; // S1854: previous assignment to 'threshold' is never used

        var products = await _db.Products
            .Where(p => p.Stock < threshold && p.IsActive)
            .ToListAsync();

        return products;
    }

    // S4830: Using HttpClient without IHttpClientFactory — not disposed properly (Code Smell / Bug)
    public async Task<string> FetchExternalPricingAsync(int productId)
    {
        var client = new HttpClient();
        var response = await client.GetAsync($"https://pricing-service.internal/api/products/{productId}");
        return await response.Content.ReadAsStringAsync();
    }
}
