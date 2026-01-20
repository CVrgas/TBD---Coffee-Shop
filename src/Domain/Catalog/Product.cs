using System.ComponentModel.DataAnnotations.Schema;
using Domain.Base;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Domain.Catalog;

public sealed class Product : EntityWithRowVersion<int>
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public CurrencyCode Currency { get; set; } = new ("USD");
    public bool IsOnSale { get; set; }
    
    [Precision(18,2)]
    public decimal? SalePrice { get; set; }

    // Inventory
    //public int StockQuantity { get; set; }
    //public bool IsBackorderAllowed { get; set; }
    public string? Description { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int RatingCount { get; set; }
    [Precision(18,2)]
    public decimal RatingSum { get; set; }
    [NotMapped]
    public decimal AverageRating => RatingCount == 0 ? 0 : RatingSum / RatingCount;
    public int CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public IEnumerable<StockItem>? StockItems { get; set; }
    public IEnumerable<StockMovement>? StockMovements { get; set; }
}