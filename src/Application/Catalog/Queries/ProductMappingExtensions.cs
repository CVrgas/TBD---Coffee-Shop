using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Domain.Catalog;

namespace Application.Catalog.Queries;

internal static class ProductMappingExtensions
{
    public static readonly Expression<Func<Product, ProductDto>> ProductDtoProjection = p => new ProductDto
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
        CategoryName = p.Category!.Name,
        RowVersion = p.RowVersion
    };
}
