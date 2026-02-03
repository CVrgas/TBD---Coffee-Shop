using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Common;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

public class CategoryQueryService(ApplicationDbContext context) : ICategoryQueryService
{
    private readonly IQueryable<ProductCategory> _query = context.ProductCategories.AsNoTracking();
    
    public async Task<ProductCategoryDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _query
            .Where(c => c.Id == id)
            .Select(_getCategorySelector)
            .FirstOrDefaultAsync(cancellationToken: ct);
        
        return category == null 
            ? null 
            : category with { Breadcrumb = await BuildBreadCrumbAsync(category.Id, ct) };
    }
    public async  Task<ProductCategoryDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var category = await _query
            .Where(c => c.Slug == slug)
            .Select(_getCategorySelector)
            .FirstOrDefaultAsync(cancellationToken: ct);
        
        return category == null 
            ? null 
            : category with { Breadcrumb = await BuildBreadCrumbAsync(category.Id, ct) };
    }
    public async Task<Paginated<ProductCategoryDto>> GetAllAsync(PaginatedRequest pagination, CancellationToken ct = default)
    {
        var querable = _query
            .Where(c => string.IsNullOrWhiteSpace(pagination.QueryPattern) || c.Name.Contains(pagination.QueryPattern))
            .ApplySort(pagination.SortOption);
        
        var totalCount = await querable.CountAsync(ct);
        
        var skip =  (pagination.ClampIndex - 1) * pagination.ClampSize;
        querable = querable.Skip(skip).Take(pagination.PageSize);

        var categories = await querable.Select(_getCategorySelector).ToListAsync(cancellationToken: ct);
        return new Paginated<ProductCategoryDto>(Entities: categories, TotalCount: totalCount, PageNumber: pagination.PageIndex, PageSize: pagination.PageSize);
    }

    #region Helpers

    private async Task<IReadOnlyList<CategoryCrumb>> BuildBreadCrumbAsync(int id, CancellationToken ct = default)
    {
        var stack = new Stack<CategoryCrumb>();
        Expression<Func<ProductCategory, CategoryCrumb>> selector = c => new CategoryCrumb{ Id = c.Id, Name = c.Name, Slug = c.Slug, ParentId = c.ParentId };

        var node = await _query
            .Where(c => c.Id == id)
            .Select(selector: selector)
            .FirstOrDefaultAsync(cancellationToken: ct);
        
        while (node is not null)
        {
            stack.Push(node);
            
            if(!node.ParentId.HasValue) node = null;
            else
            {
                node = await _query.Where(c => c.Id == node.ParentId.Value)
                    .Select(selector: selector)
                    .FirstOrDefaultAsync(cancellationToken: ct);;
            }
        }
        return stack.ToList();
    }
    
    private readonly Expression<Func<ProductCategory, ProductCategoryDto>> _getCategorySelector = productCategory => new ProductCategoryDto
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

    #endregion
}