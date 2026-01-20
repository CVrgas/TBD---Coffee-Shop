using Api.Common.Idempotency;
using Api.Middlewares;
using Application.Common.Abstractions.Envelope;
using Application.Payments.Services;

namespace Api.Modules.Payment;

/// <summary>
/// Represents a request to confirm a payment intent.
/// </summary>
/// <param name="IntentId">The unique identifier of the payment intent to confirm.</param>
/// <param name="OrderNumber">The order number associated with the payment.</param>
public sealed record PayConfirmationRequest(string IntentId, string OrderNumber);

/// <summary>
/// Represents a request to create a new payment intent.
/// </summary>
/// <param name="OrderNumber">The order number associated with the payment.</param>
public sealed record PayCreateRequest(string OrderNumber);

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
        var group = endpoints.MapGroup("/payment")
            .RequireAuthorization()
            .WithTags("Payment");

        group.MapPost("/confirm", async (PayConfirmationRequest req, IPaymentIntentService paymentIntentService) => 
                await paymentIntentService.ConfirmPaymentIntentAsync(req.IntentId, req.OrderNumber))
            .AddEndpointFilter(new IdempotentEndpointFilter())
            .WithSummary("confirmed payment intent");
        
        group.MapPost("/create", async (PayCreateRequest req, IPaymentIntentService paymentIntentService) => 
                await paymentIntentService.CreatePaymentIntentAsync(req.OrderNumber))
            .WithSummary("Create payment intent");
        
        return endpoints;

    }
    
}