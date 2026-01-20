namespace Application.Common.Interfaces.Payment;

public sealed record PaymentConfirmationResult(string IntentId, string ClientSecret, bool IsSuccess, string? ErrorMessage = null);