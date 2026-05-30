using Api.Common.Idempotency;
using Api.Middlewares;
using Application.Catalog.Commands.Create;
using Application.Catalog.Commands.Rate;
using Application.Catalog.Commands.ToggleStatus;
using Application.Catalog.Commands.Update;
using Application.Catalog.Commands.UpdatePrice;
using Application.Catalog.Queries.GetProductById;
using Application.Catalog.Queries.GetProductBySku;
using Application.Catalog.Queries.GetProductsPaginated;
using Application.Catalog.Queries.GetProductStockItems;
using Application.Common.Abstractions.Persistence.Paginated;
using Infrastructure.Caching;
using Infrastructure.Integration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Defines the endpoints for product operations.
/// </summary>
public static class CatalogEndpoints
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
        
        group.MapGet("/", async ([AsParameters] PaginatedRequest req, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetProductsPaginatedQuery(req), cancellationToken);
                return result;
            })
            .CacheOutput(CachePolicy.Catalog.Name)
            .WithSummary("Get all products paginated");
        
        group.MapPost("/", async ([FromBody] CreateProductCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            await sender.Send(request, cancellationToken))
            .RequireAuthorization(AuthorizationPolicy.Admin.Name)
            .AddEndpointFilter(new ValidationFilter<CreateProductCommand>())
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .InvalidateCacheTag(CachePolicy.Catalog.Name)
            .WithSummary("Create a new product catalog entry");
        
        group.MapPut("/{productId:int}", async (int productId, [FromBody] UpdateProductCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            {
                request.SetProductId(productId);
                return await sender.Send(request, cancellationToken);
            })
            .RequireAuthorization()
            .InvalidateCacheTag(CachePolicy.Catalog.Name)
            .AddEndpointFilter(new ValidationFilter<UpdateProductCommand>())
            .WithSummary("Update an existing product's complete details");
        
        group.MapPatch("/{productId:int}/price", async (int productId, [FromBody] UpdatePriceCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(request, cancellationToken))
            .RequireAuthorization(AuthorizationPolicy.Admin.Name)
            .InvalidateCacheTag(CachePolicy.Catalog.Name)
            .WithSummary("Modify the price of a specific product");
        
        group.MapPatch("/{productId:int}/status", async (int productId, [FromBody] ToggleStatusCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(request, cancellationToken))
            .RequireAuthorization(AuthorizationPolicy.Admin.Name)
            .InvalidateCacheTag(CachePolicy.Catalog.Name)
            .WithSummary("Toggle the active/inactive status of a product");
        
        group.MapPost("/{productId:int}/ratings", async (int productId, [FromBody] RateProductCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(request, cancellationToken))
            .RequireAuthorization()
            .InvalidateCacheTag(CachePolicy.Catalog.Name)
            .WithSummary("Submit a user rating for a product");

        group.MapGet("{id:int}", async (int id, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
                return result;
            })
            .CacheOutput(CachePolicy.Catalog.Name)
            .WithSummary("Get product by its ID");
        
        group.MapGet("{sku}", async (string sku, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetProductBySkuQuery(sku), cancellationToken);
                return result;
            })
            .CacheOutput(CachePolicy.Catalog.Name)
            .WithSummary("Get product by Sku");
        
        group.MapGet("list/{id:int}", async (int id, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(new GetProductStockItemsQuery(id), cancellationToken);
                return result;
            })
            .WithSummary("Get Stock Item");

        return endpoints;
    }
}