using McpDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace McpDemo.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var seededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories", CreatedAt = seededAt },
            new Category { Id = 2, Name = "Clothing", Description = "Apparel and fashion", CreatedAt = seededAt },
            new Category { Id = 3, Name = "Books", Description = "Books and media", CreatedAt = seededAt }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Wireless Headphones", Sku = "ELEC-001", Price = 79.99m, Stock = 50, CategoryId = 1, Description = "Noise-cancelling over-ear headphones", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Product { Id = 2, Name = "Mechanical Keyboard", Sku = "ELEC-002", Price = 129.99m, Stock = 30, CategoryId = 1, Description = "Tactile mechanical switches, RGB backlight", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Product { Id = 3, Name = "Running Shoes", Sku = "CLTH-001", Price = 89.99m, Stock = 75, CategoryId = 2, Description = "Lightweight trail running shoes", CreatedAt = seededAt, UpdatedAt = seededAt },
            new Product { Id = 4, Name = "Clean Code", Sku = "BOOK-001", Price = 34.99m, Stock = 100, CategoryId = 3, Description = "A handbook of agile software craftsmanship by Robert C. Martin", CreatedAt = seededAt, UpdatedAt = seededAt }
        );
    }
}
