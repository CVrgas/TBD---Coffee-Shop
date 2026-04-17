using Api.Common.Idempotency;
using Api.Middlewares;
using Application.Inventory.Commands.AdjustStock;
using Application.Inventory.Commands.GetStockLevel;
using Infrastructure.Caching;
using Infrastructure.Integration;
using MediatR;

namespace Api.Modules.Inventory;

/// <summary>
/// Defines the endpoints for inventory operations.
/// </summary>
public static class InventoryEndpoints
{
    /// <summary>
    /// Maps the inventory endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with inventory endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/inventories")
            .RequireAuthorization(AuthorizationPolicy.ElevatedRights.Name)
            .WithTags("Inventory");
        
        group.MapGet("/{productId:int}", async (int productId, ISender sender, CancellationToken cancellationToken = default) => 
                await sender.Send(new GetStockLevelCommand(productId), cancellationToken))
            .CacheOutput(CachePolicy.Inventory.Name)
            .WithSummary("Get Stock Level");

        group.MapPost("/adjust", async (AdjustStockCommand request, ISender sender, CancellationToken cancellationToken = default) => 
                await sender.Send(request, cancellationToken))
            .AddEndpointFilter(new ValidationFilter<AdjustStockCommand>())
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .InvalidateCacheTag(CachePolicy.Inventory.Name)
            .WithSummary("Stock Movement");
        
        return endpoints;
    }
}