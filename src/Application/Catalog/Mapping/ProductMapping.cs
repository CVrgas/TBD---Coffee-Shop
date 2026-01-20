using Application.Catalog.Dtos;
using Domain.Base;
using Domain.Catalog;

namespace Application.Catalog.Mapping;

public static class ProductMapping 
{
    public static ProductDto ToDto(this Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Sku = p.Sku,
            Name = p.Name,
            Price = p.Price,
            Currency = p.Currency.Code,
            IsOnSale = p.IsOnSale,
            SalePrice = p.SalePrice,
            Description = p.Description ?? "",
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            RatingCount = p.RatingCount,
            RatingSum = p.RatingSum,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            RowVersion = p.RowVersion
        };
    }

    public static Product ToEntity(this ProductDto dto)
    {
        return new Product
        {
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Sku = dto.Sku,
            Name = dto.Name,
            Price = dto.Price,
            Currency = dto.Currency is not null ? new CurrencyCode(dto.Currency) : throw new NullReferenceException("Currency not found"),
            IsOnSale = dto.IsOnSale,
            SalePrice = dto.SalePrice,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            IsActive = dto.IsActive,
            RatingCount = dto.RatingCount,
            RatingSum = dto.RatingSum,
            CategoryId = dto.CategoryId ?? throw new NullReferenceException("Category not found")
        };
    }

    public static Product ToEntity(this ProductCreateDto p, string sku)
    {
        return new Product
        {
            Name = p.Name!,
            Sku = sku,
            Price = p.Price,
            Currency = new CurrencyCode(p.Currency!),
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            CategoryId = p.CategoryId,
        };
    }
}