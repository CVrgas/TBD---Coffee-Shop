using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Inventory.Dtos;
using Infrastructure.Caching;
using Infrastructure.Persistence.Configurations;

namespace Api.Modules.Catalog;

/// <summary>
/// Defines the endpoints for product operations.
/// </summary>
public static class ProductEndpoints
{
    /// <summary>
    /// Maps the product endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with product endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapProduct(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/products")
            .WithTags("Products");
        
        group.MapGet("/", async ([AsParameters] PaginatedRequest req, IProductQueryService service) =>
            {
                var result = await service.PaginatedAsync(request: req);
                return Envelope<Paginated<ProductDto>>.Ok(result);
            })
            .CacheOutput(CachePolicies.Catalog)
            .WithSummary("Get all products paginated");

        group.MapGet("{id:int}", async (int id, IProductQueryService service) =>
            {
                var result = await service.GetByIdAsync(id);
                return result is null ? Envelope<ProductDto>.NotFound() : Envelope<ProductDto>.Ok(result);
            })
            .CacheOutput(CachePolicies.Catalog)
            .WithSummary("Get product by its ID");
        
        group.MapGet("{sku}", async (string sku, IProductQueryService service) =>
            {
                var result = await service.GetBySkuAsync(sku);
                return result is null ? Envelope<ProductDto>.NotFound() : Envelope<ProductDto>.Ok(result);
            })
            .CacheOutput(CachePolicies.Catalog)
            .WithSummary("Get product by Sku");
        
        group.MapGet("list/{id:int}", async (int id, ICatalogQueries service) =>
            {
                var list = await service.GetStockItemsAsync(id);
                return Envelope<List<StockItemDto>>.Ok(list);
            })
            .WithSummary("Get Stock Item");

        return endpoints;
    }
}