using Domain.Base.Entities;
using Domain.Base.ValueObjects;

namespace Domain.Catalog;

public sealed class Product : EntityWithRowVersion<int>
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? Sku { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public bool IsOnSale { get; private set; }
    public decimal? SalePrice { get; private set; }
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
        var formattedName = name?.Trim();
        var formattedDescription = description?.Trim();
        if (string.IsNullOrWhiteSpace(formattedName) && string.IsNullOrWhiteSpace(formattedDescription) && categoryId is null or 0) throw new ArgumentException("No changes", nameof(name));
        
        Name = string.IsNullOrWhiteSpace(formattedName) ? Name : formattedName;
        Description = string.IsNullOrWhiteSpace(formattedDescription) ? Description : formattedDescription;
        CategoryId = categoryId ?? CategoryId;
        MarkAsUpdated();
    }

    private void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void PutOnSale(decimal salePrice)
    {
        if (salePrice <= 0) throw new ArgumentOutOfRangeException(nameof(salePrice), "Sale price must be strictly positive");
        if (salePrice >= Price) throw new ArgumentException("Sale price must be less than regular price", nameof(salePrice));
        
        SalePrice = salePrice;
        IsOnSale = true;
        MarkAsUpdated();
    }
    
    public void EndSale()
    {
        SalePrice = null;
        IsOnSale = false;
        MarkAsUpdated();
    }
}
