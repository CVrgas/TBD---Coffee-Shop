namespace Application.Catalog.Dtos;

public record ProductCategoryCreateDto(string Name, string Code, string? Description, int? ParentId);