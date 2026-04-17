using Application.Common.Interfaces.Payment;

namespace Infrastructure.Payment;

public class MockPaymentGateway : IPaymentGateway
{
    private enum IntentState
    {
        Success, // Success 
        Insufficient, // Declined
        Fraud  // Fraud
    }

    private readonly Dictionary<string, string> _dict = new()
    {
        { nameof(IntentState.Insufficient), "Insufficient funds" },
        { nameof(IntentState.Fraud), "Fraud Detected" }
    };
    
    public async Task<PaymentIntentResult> CreateIntentAsync(string orderNumber, decimal amount, string currency, CancellationToken ct = default)
    {
        var intent = GetMockIntent(amount);
        await Task.Delay(500, ct);
        return new PaymentIntentResult(intent, $"{intent}_secret_{orderNumber}");
    }

    public async Task<PaymentConfirmationResult> ConfirmPaymentAsync(string intent, CancellationToken ct = default)
    {
        var intentProps = intent.Split("_");
        var isSuccess = intentProps is [_, "Success", ..];
        string? errorMessage = null;

        if (!isSuccess)
        {
            errorMessage = _dict.GetValueOrDefault(intentProps[1], "Unknown error.");
        }
        
        await Task.Delay(1000, ct);
        return new PaymentConfirmationResult(intent, "Cliente_secret", isSuccess, errorMessage);
    }
    
    private string GetMockIntent(decimal amount)
    {
        var prefix = "pi_";
        var suffix = Random.Shared.Next(0, 9999).ToString("0000");
        
        if (amount >= 1000) prefix += nameof(IntentState.Fraud);
        else if (amount % 1 == 0.99m) prefix += nameof(IntentState.Insufficient);
        else prefix += nameof(IntentState.Success);
        
        return string.Concat(prefix, "_mock_" ,suffix);
    }
}