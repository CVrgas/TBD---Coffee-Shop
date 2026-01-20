namespace Application.Catalog.Dtos;

public sealed record ProductUpdateDto(
    int ProductId, 
    string? Name,
    string? Description,
    int? CategoryId, 
    byte[]? RowVersion);