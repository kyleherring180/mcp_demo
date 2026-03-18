namespace McpDemo.Api.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    int ProductCount,
    DateTime CreatedAt
);

public record CreateCategoryRequest(
    string Name,
    string? Description
);

public record UpdateCategoryRequest(
    string Name,
    string? Description
);
