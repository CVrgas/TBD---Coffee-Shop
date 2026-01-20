using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces.Payment;

namespace Application.Payments.Services;

public interface IPaymentIntentService
{
    Task<Envelope<PaymentConfirmationResult>> CreatePaymentIntentAsync(string orderNumber, CancellationToken ct = default);
    Task<Envelope> ConfirmPaymentIntentAsync(string intentId, string orderNumer, CancellationToken ct = default);
}