using Application.Catalog.Commands.Create;
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
        return Product.Create(
            sku: dto.Sku ?? throw new ArgumentNullException(nameof(dto.Sku)),
            name: dto.Name,
            price: dto.Price,
            currencyCode: dto.Currency ?? throw new ArgumentNullException(nameof(dto.Currency)),
            description: dto.Description,
            imageUrl: dto.ImageUrl,
            categoryId: dto.CategoryId ?? throw new NullReferenceException("Category not found")
        );
    }

    public static Product ToEntity(this CreateProductCommand p, string sku)
    {
        return Product.Create(
            name: p.Name,
            sku: sku,
            price: p.Price,
            currencyCode: p.Currency,
            description: p.Description,
            imageUrl: p.ImageUrl,
            categoryId: p.CategoryId
            );
    }
}