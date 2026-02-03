using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

public class ProductQueryService(ApplicationDbContext dbContext) : IProductQueryService
{
    private readonly IQueryable<Product> _query = dbContext.Products.AsNoTracking();
    private readonly IQueryable<ProductCategory> _queryCategory = dbContext.ProductCategories.AsNoTracking();
    
    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _query
            .Where(p => p.Id == id)
            .Select(_getProductSelector)
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _query
            .Where(p => p.Sku == sku)
            .Select(_getProductSelector)
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<Paginated<ProductDto>> GetByCategoryAsync(string category, int pageSize = 10, int pageNumber = 0, CancellationToken cancellationToken = default)
    {
        var categoryId = await _queryCategory
            .Where(c => c.Name == category)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        
        if(categoryId == 0) return new Paginated<ProductDto>([], 0, pageNumber, pageSize);

        var filteredQuery = _query.Where(p => p.CategoryId == categoryId);
        
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        
        var skip = (pageNumber - 1) * pageSize;
        var products = await filteredQuery.Skip(skip).Take(pageSize)
            .Select(_getProductSelector)
            .ToListAsync(cancellationToken);
        
        return new Paginated<ProductDto>(products, totalCount, pageNumber, pageSize);
    }
    public async Task<Paginated<ProductDto>> PaginatedAsync(PaginatedRequest request, CancellationToken cancellationToken = default)
    {
        var querable = _query
            .Where(c =>
                string.IsNullOrWhiteSpace(request.QueryPattern) || c.Name.Contains(request.QueryPattern) &&
                !request.OnlyActive || c.IsActive)
            .ApplySort(request.SortOption);
        
        var totalCount = await querable.CountAsync(cancellationToken);
        
        querable = querable.Skip(request.Skip).Take(request.PageSize);
        
        var products = await querable.Select(_getProductSelector).ToListAsync(cancellationToken);
        
        return new Paginated<ProductDto>(products, totalCount, request.PageIndex, request.PageSize);
    }
    
    private readonly Expression<Func<Product, ProductDto>> _getProductSelector = p => new ProductDto
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