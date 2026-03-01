using Domain.Base;

namespace Domain.Catalog;

public sealed class Product : EntityWithRowVersion<int>
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? Sku { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public bool IsOnSale { get; set; }
    public decimal? SalePrice { get; set; }
    public CurrencyCode Currency { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public int RatingCount { get; private set; }
    public decimal RatingSum { get; private set; }
    public decimal AverageRating => RatingCount == 0 ? 0 : RatingSum / RatingCount;
    public int CategoryId { get; private set; }
    public ProductCategory? Category { get; private set; }
    
    private Product() { }
    
    public static Product Create(string name, string sku, decimal price, string currencyCode, int categoryId, string? description = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Price must be strictly positive");
        
        return new Product
        {
            Name = name,
            Sku = sku,
            Price = price,
            Currency = new CurrencyCode(currencyCode),
            CategoryId = categoryId,
            Description = description,
            ImageUrl = imageUrl,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    
    public void UpdatePrice(decimal newPrice, CurrencyCode newCurrency)
    {
        if (newPrice <= 0) throw new ArgumentOutOfRangeException(nameof(newPrice), "Price must be strictly positive");
        
        Price = newPrice;
        Currency = newCurrency;
        MarkAsUpdated();
    }

    public void AddRating(int rate)
    {
        if (rate is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(rate), "Rating must be between 1 and 5");
        
        RatingSum += rate;
        RatingCount++;
    }

    public void ToggleStatus(bool? newState = null)
    {
        IsActive = newState ?? !IsActive;
        MarkAsUpdated();
    }

    public void UpdateDetails(string? name, string? description, int? categoryId)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description) && categoryId is null or 0) throw new ArgumentException("No changes", nameof(name));
        
        Name = name ?? Name;
        Description = description ?? Description;
        CategoryId = categoryId ?? CategoryId;
        MarkAsUpdated();
    }

    private void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
