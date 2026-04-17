using Api.Common.Idempotency;
using Api.Middlewares;
using Application.Payments.Commands.ConfirmPayment;
using Application.Payments.Commands.CreatePaymentIntent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Payment;

/// <summary>
/// Defines the endpoints for payment operations.
/// </summary>
public static class PaymentEndpoints
{
    /// <summary>
    /// Maps the payment endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with payment endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapPayment(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/payments")
            .RequireAuthorization()
            .WithTags("Payment");
        
        group.MapPost("/confirm", async ([FromBody] ConfirmPaymentCommand req, [FromServices] ISender sender, CancellationToken ct) => 
            await sender.Send(req, ct))
            .AddEndpointFilter(new ValidationFilter<ConfirmPaymentCommand>())
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .WithSummary("confirmed payment intent");
        
        group.MapPost("/intents", async ([FromBody] CreatePaymentIntentCommand req, [FromServices] ISender sender) => 
            await sender.Send(req, CancellationToken.None))
            .AddEndpointFilter(new ValidationFilter<CreatePaymentIntentCommand>())
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .WithSummary("Create payment intent");
        
        return endpoints;

    }
    
}