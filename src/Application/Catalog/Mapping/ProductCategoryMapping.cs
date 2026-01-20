using Application.Catalog.Dtos;
using Domain.Catalog;

namespace Application.Catalog.Mapping;

public static class ProductCategoryMapping
{
    public static ProductCategoryDto ToDto(this ProductCategory productCategory)
    {
        return new ProductCategoryDto
        {
            Id = productCategory.Id,
            Name = productCategory.Name,
            Slug = productCategory.Slug,
            Code = productCategory.Code,
            Description = productCategory.Description,
            IsActive = productCategory.IsActive,
            CreatedAt = productCategory.CreatedAt,
            UpdatedAt = productCategory.UpdatedAt,
        };
    }
}