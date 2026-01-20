namespace Application.Catalog.Dtos;

public record ProductUpdatePrice(int Id, decimal Price, string Currency, byte[]? RowVersion); 