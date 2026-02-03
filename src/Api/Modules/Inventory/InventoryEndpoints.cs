using Api.Middlewares;
using Application.Inventory.Dtos;
using Application.Inventory.Interfaces;
using Infrastructure.Integration;
using Microsoft.AspNetCore.Mvc;

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
        var group = endpoints.MapGroup("/inventory")
            .RequireAuthorization(AuthPolicyName.ElevatedRights)
            .WithTags("Inventory");

        group.MapGet("/product/{id:int}", async (int id, IInventoryService service) => 
                await service.GetStockItemsAsync(id))
            .WithSummary("Get Stock Item");

        group.MapPost("/adjust", async (AdjustStockDto dto, IInventoryService service) => 
                await service.AdjustStock(dto))
            .AddEndpointFilter(new ValidationFilter<AdjustStockDto>())
            .WithSummary("Stock Movement");
        
        return endpoints;
    }
}