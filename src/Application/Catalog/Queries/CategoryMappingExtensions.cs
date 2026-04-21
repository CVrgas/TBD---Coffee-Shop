using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Common;
using Application.Common.Interfaces;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries;

internal static class CategoryMappingExtensions
{
    public static readonly Expression<Func<ProductCategory, ProductCategoryDto>> CategoryDtoProjection = productCategory => new ProductCategoryDto
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

    public static async Task<IReadOnlyList<CategoryCrumb>> BuildBreadCrumbAsync(IAppDbContext context, int id, CancellationToken ct = default)
    {
        var stack = new Stack<CategoryCrumb>();
        Expression<Func<ProductCategory, CategoryCrumb>> selector = c => new CategoryCrumb{ Id = c.Id, Name = c.Name, Slug = c.Slug, ParentId = c.ParentId };

        var node = await context.ProductCategories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken: ct);
        
        while (node is not null)
        {
            stack.Push(node);
            
            if(!node.ParentId.HasValue) node = null;
            else
            {
                node = await context.ProductCategories.AsNoTracking()
                    .Where(c => c.Id == node.ParentId.Value)
                    .Select(selector)
                    .FirstOrDefaultAsync(cancellationToken: ct);
            }
        }
        return stack.ToList();
    }
}
