using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Inventory.Abstractions;
using Application.Inventory.Dtos;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Abstractions;

public class CatalogQueries(ApplicationDbContext context) : ICatalogQueries
{
    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(_getProductSelector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await context.Products.AsNoTracking()
            .Where(p => p.Sku == sku)
            .Select(_getProductSelector)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<Paginated<ProductDto>> PaginatedAsync(PaginatedRequest request, CancellationToken cancellationToken = default)
    {
        var queryable = context.Products.AsNoTracking()
            .Where(c =>
                (!request.OnlyActive || c.IsActive)  &&
                (string.IsNullOrWhiteSpace(request.QueryPattern) || c.Name.Contains(request.QueryPattern)))
            .ApplySort(request.SortOption);
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        queryable = queryable.Skip(request.Skip).Take(request.PageSize);
        
        var products = await queryable.Select(_getProductSelector).ToListAsync(cancellationToken);
        return new Paginated<ProductDto>(products, totalCount, request.PageIndex, request.PageSize);
    }

    public async Task<Paginated<ProductDto>> GetByCategoryAsync(PaginatedRequest request, CancellationToken cancellationToken = default)
    {
        var queryable = context.Products.AsNoTracking()
            .Where(c => 
                (!request.OnlyActive || c.IsActive) && 
                (
                    string.IsNullOrWhiteSpace(request.QueryPattern) || 
                    c.Category == null || 
                    c.Category.Slug == request.QueryPattern) 
                )
            .AsQueryable();
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        queryable = queryable.Skip(request.Skip).Take(request.PageSize);
        
        var products = await queryable.Select(_getProductSelector).ToListAsync(cancellationToken);
        return new Paginated<ProductDto>(products, totalCount, request.PageIndex, request.PageSize);
    }
    
    public async Task<List<StockItemDto>> GetStockItemsAsync(int productId, CancellationToken ct = default)
    {
        return await context.StockItems
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(item => new StockItemDto(item.ProductId, item.QuantityOnHand, item.ReservedQuantity, item.IsActive, item.RowVersion))
            .ToListAsync(cancellationToken: ct);
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