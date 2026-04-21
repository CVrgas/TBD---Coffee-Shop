using Api.Common.Idempotency;
using Api.Middlewares;
using Application.Orders.Commands.CancelOrder;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Queries.GetOrderByNumber;
using Application.Orders.Queries.GetPaginatedOrder;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

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
            .MapGroup("/orders")
            .RequireAuthorization()
            .WithTags("Orders");

        group.MapGet("/", async ([AsParameters] GetPaginatedOrderCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
                await sender.Send(request, cancellationToken))
            .WithSummary("Get Order");
        
        group.MapPost("/", async ([FromBody] CreateOrderCommand request, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(request, cancellationToken))
            .AddEndpointFilter(new ValidationFilter<CreateOrderCommand>())
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .WithSummary("Create a new order and reserve stock synchronously");
        
        group.MapGet("/{orderNumber}", async (string orderNumber, int orderId,[FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(new GetOrderByNumberQuery(orderNumber), cancellationToken))
            .WithSummary("Get Order");

        group.MapPost("/{orderNumber}/cancel", async (string orderNumber, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
                await sender.Send(new CancelOrderCommand(OrderNumber: orderNumber), cancellationToken))
            .WithSummary("Cancel a Order");
        
        return endpoints;
    }
}