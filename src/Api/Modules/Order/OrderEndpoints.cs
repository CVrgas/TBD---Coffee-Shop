using System.Security.Claims;
using Api.Common.Idempotency;
using Api.Middlewares;
using Application.Common.Abstractions.Envelope;
using Application.Orders.Dtos;
using Application.Orders.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Order;

/// <summary>
/// Defines the endpoints for order operations.
/// </summary>
public static class OrderEndpoints
{
    /// <summary>
    /// Maps the order endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with order endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapOrder(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/order")
            .RequireAuthorization()
            .WithTags("Order");
        
        group.MapGet("/all", async ([FromQuery]int? userId,  IOrderService service) => 
                await service.GetOrdersAsync(userId))
            .WithSummary("Get Order");
        
        group.MapPost("/add", async ([FromBody] OrderCreationDto order, IOrderService service) => 
                await service.AddOrderAsync(order))
            .AddEndpointFilter(new ValidationFilter<OrderCreationDto>())
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .WithSummary("Create a Order");

        group.MapPost("/cancel", async ([FromBody] string orderNumber, IOrderService service) => 
                await service.CancelOrderAsync(orderNumber))
            .WithSummary("Cancel a Order");
        
        return endpoints;
    }
}