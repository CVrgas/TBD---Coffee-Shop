namespace Application.Common.Interfaces.Payment;

public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreateIntentAsync(string orderNumber, decimal amount, string currency, CancellationToken ct = default);
    Task<PaymentConfirmationResult> ConfirmPaymentAsync(string intent, CancellationToken ct = default);
}