using Api.Middlewares;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence.Paginated;
using Infrastructure.Caching;
using Infrastructure.Integration;

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
                var result = await service.PaginatedAsync(req.ClampIndex, req.ClampSize);
                return Results.Ok(result);
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

        #region Admin
        
        group.MapPost("/", async (ProductCreateDto dto, IProductService svc) =>
                await svc.AddAsync(dto))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .AddEndpointFilter(new ValidationFilter<ProductCreateDto>())
            .WithSummary("Create a new product");

        group.MapPut("/", async (ProductUpdateDto dto, IProductService svc) =>
                await svc.UpdateAsync(dto))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .AddEndpointFilter(new ValidationFilter<ProductUpdateDto>())
            .WithSummary("Update a product");

        group.MapDelete("{id:int}", async (int id, IProductService svc) =>
                await svc.DeactiveProduct(id))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .WithSummary("Delete product");
        
        group.MapPatch("{id:int}/status", async (int id, IProductService svc) =>
                await svc.ToggleStatus(id))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .WithSummary("toggle a product status");
        
        group.MapPatch("{id:int}/price", async (int id, ProductUpdatePrice dto, IProductService svc, CancellationToken ct) => 
                await svc.UpdatePrice(dto with { Id = id }, ct))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .WithSummary("Update product price")
            .WithDescription("Updates only the price (and currency) of a product by ID. Supports optimistic concurrency.")
            .Produces<Envelope<ProductDto>>(StatusCodes.Status200OK)
            .Produces<Envelope>(StatusCodes.Status404NotFound)
            .Produces<Envelope>(StatusCodes.Status409Conflict);

        group.MapPost("{id:int}/images", async (int id, string imageUrl, IProductService svc) => 
                await svc.UpdateImageAsync(id, imageUrl))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .WithSummary("Update product image");
        
        group.MapPost("bulk", async (List<ProductCreateDto> dtos, IProductService svc) =>
                await svc.BulkCreateAsync(dtos))
            .RequireAuthorization(AuthPolicyName.Admin)
            .InvalidateCacheTag(CachePolicies.Catalog)
            .AddEndpointFilter(new ValidationFilter<List<ProductCreateDto>>())
            .WithSummary("Bulk products");
        
        group.MapGet("{id:int}/history", (int id, IProductService svc) => { throw new NotImplementedException(); });

        #endregion

        return endpoints;
    }
}