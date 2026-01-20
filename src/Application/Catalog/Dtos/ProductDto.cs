namespace Application.Catalog.Dtos;

public sealed record ProductDto
{
    public int Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = null!;
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public bool IsOnSale { get; init; }
    public decimal? SalePrice { get; init; }
    public string Description { get; init; } = null!;
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
    public int RatingCount { get; init; }
    public decimal RatingSum { get; init; }
    public decimal AverageRating => RatingCount > 0 ? RatingSum / RatingCount : 0;
    public int? CategoryId { get; init; }
    public string? CategoryName { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}