using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Inventory.Abstractions;

namespace Infrastructure.Persistence.Queries;

public class ProductQueryService(IInventoryQueries inventoryQueries, ICatalogQueries catalogQueries) : IProductQueryService
{
    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await catalogQueries.GetByIdAsync(id, cancellationToken);
        if (product is null) return product;
        
        var stock = await inventoryQueries.GetAvailableStock(product.Id, cancellationToken);
        product.SetQuantityInStock(stock);

        return product;
    }
    public async Task<Paginated<ProductDto>> PaginatedAsync(PaginatedRequest request, CancellationToken cancellationToken = default)
    {

        var page = await catalogQueries.PaginatedAsync(request, cancellationToken);
        var productsIds = page.Entities.Select(p => p.Id).ToHashSet();
        
        var stocks = await inventoryQueries.GetAvailableStock(productsIds, cancellationToken);

        foreach (var product in page.Entities)
        {
            if(stocks.TryGetValue(product.Id, out var stock)) product.SetQuantityInStock(stock);
        }
        
        return page;
    }
    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var product = await catalogQueries.GetBySkuAsync(sku, cancellationToken);
        if (product is null) return product;
        
        var stock = await inventoryQueries.GetAvailableStock(product.Id, cancellationToken);
        product.SetQuantityInStock(stock);

        return product;
    }
    public async Task<Paginated<ProductDto>> GetByCategoryAsync(PaginatedRequest request, CancellationToken cancellationToken = default)
    {
        var page = await catalogQueries.GetByCategoryAsync(request: request ,cancellationToken: cancellationToken);
        var productsIds = page.Entities.Select(p => p.Id).ToHashSet();
        
        var stocks = await inventoryQueries.GetAvailableStock(productsIds, cancellationToken);

        foreach (var product in page.Entities)
        {
            if(stocks.TryGetValue(product.Id, out var stock)) product.SetQuantityInStock(stock);
        }
        
        return page;
    }
}