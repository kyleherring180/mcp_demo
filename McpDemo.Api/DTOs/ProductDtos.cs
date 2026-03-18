namespace McpDemo.Api.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int Stock,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateProductRequest(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int Stock,
    int CategoryId
);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    bool IsActive,
    int CategoryId
);

public record ProductSearchRequest(
    string? Query,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? InStockOnly
);
