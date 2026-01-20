using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Services;

public class ProductCategoryService (IRepository<ProductCategory, int> repository, IUnitOfWork uoW) : IProductCategoryService
{
    public async Task<Envelope<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        ProductCategory productCategory = new ProductCategory();
        var category = await repository.GetByIdAsync(id, selector: _getCategorySelector,  ct: ct);
        if(category == null) return Envelope<ProductCategoryDto>.NotFound();

        var breadcrumbs = await BuildBreadCrumbAsync(category.Id, ct);
        var dto = category with { Breadcrumb = breadcrumbs };

        return Envelope<ProductCategoryDto>.Ok(dto);
    }

    public async Task<Envelope<ProductCategoryDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var category = await repository.GetAsync(c => c.Slug == slug, selector: _getCategorySelector, ct: ct);
        if(category == null) return Envelope<ProductCategoryDto>.NotFound();
        
        var breadcrumbs = await BuildBreadCrumbAsync(category.Id, ct);
        var dto = category with { Breadcrumb = breadcrumbs };

        return Envelope<ProductCategoryDto>.Ok(dto);
    }

    public async Task<Envelope<IEnumerable<ProductCategoryDto>>> GetAllAsync(string? query = null, SortOption? sort = null, int? take = null, CancellationToken ct = default)
    {
        query = string.IsNullOrEmpty(query) ? null : query.Trim();
        var pattern = query is null ? null : $"%{Utilities.EscapeLike(query)}%";
        
        Expression<Func<ProductCategory, bool>>? predicate = query is null ? null :
            c => EF.Functions.Like(c.Name, pattern, "\\");
        
        var categories = await repository.ListAsync(selector: _getCategorySelector, predicate, sort: null, take, ct: ct);
        return Envelope<IEnumerable<ProductCategoryDto>>.Ok(categories);
    }

    public async Task<Envelope<ProductCategoryDto>> AddAsync(ProductCategoryCreateDto dto, CancellationToken ct = default)
    {
        var name = dto.Name!.Trim();
        var slug = name.Slugify();

        if (dto.ParentId.HasValue)
        {
            var parent = await repository.GetByIdAsync(dto.ParentId.Value, ct: ct);
            if (parent is null || !parent.IsActive) 
                return Envelope<ProductCategoryDto>.BadRequest()
                    .WithError(nameof(dto.ParentId), "category not found or inactive.");
        }
            
        var exist = await repository.ExistsAsync(c => c.Slug == slug, ct: ct);
        if (exist) 
            return Envelope<ProductCategoryDto>.BadRequest()
                .WithError(nameof(dto.ParentId), "Category already exists.");
            
        if(string.IsNullOrWhiteSpace(dto.Code)) return Envelope<ProductCategoryDto>.BadRequest()
            .WithError(nameof(dto.Code), "Code is required.");

        var newCategory = new ProductCategory {
            Name = name,
            Slug = slug,
            Code = dto.Code.ToUpperInvariant().Replace(" ", ""),
            Description = dto.Description,
            ParentId = dto.ParentId,
        };
            
        await repository.Create(newCategory);
        await uoW.SaveChangesAsync(ct);
        return Envelope<ProductCategoryDto>.Ok(newCategory.ToDto());
    }

    public async Task<bool> ExistAsync(Expression<Func<ProductCategory, bool>> predicate, CancellationToken ct = default)
    {
        return await repository.ExistsAsync(predicate, ct: ct);
    }
    
    private async Task<IReadOnlyList<CategoryCrumb>> BuildBreadCrumbAsync(int id, CancellationToken ct = default)
    {
        var stack = new Stack<CategoryCrumb>();
        Expression<Func<ProductCategory, CategoryCrumb>> selector = c => new CategoryCrumb{ Id = c.Id, Name = c.Name, Slug = c.Slug, ParentId = c.ParentId };
        
        var node = await repository.GetByIdAsync(id, selector: selector, ct: ct);
        
        while (node is not null)
        {
            stack.Push(node);
            
            if (node.ParentId.HasValue)
            {
                node = await repository.GetByIdAsync(node.ParentId.Value, selector: selector, ct: ct);
            }
            else
            {
                node = null;
            }
            
        }

        return stack.ToList();
    }

    public async Task<IEnumerable<ProductCategoryDto>> GetAllAsync(
        Expression<Func<ProductCategory, bool>>? predicate = null,
        SortOption? sort = null,
        int? take = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var products = await repository.ListAsync(predicate, take: take, asNoTracking: asNoTracking, ct: ct);
        return products.Select(p => p.ToDto());
    }

    #region Helpers
    
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