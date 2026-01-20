namespace Application.Common.Interfaces.Payment;

public sealed record PaymentIntentResult(string IntentId, string ClientSecret);