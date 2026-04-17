using Domain.Base;

namespace Domain.Orders;

public sealed class PaymentRecord : Entity<int>, IHasRowVersion
{
    private PaymentRecord() { }

    public static PaymentRecord Create(
        int orderId,
        PaymentProvider provider,
        string intentId,
        PaymentStatus paymentStatus,
        decimal amount,
        CurrencyCode currency,
        string clientSecret)
    {
        if(orderId <= 0) throw new ArgumentOutOfRangeException(nameof(orderId), "Order id cannot be less than zero");
        if(string.IsNullOrWhiteSpace(intentId)) throw new ArgumentNullException(nameof(intentId), "Intent id cannot be empty");
        if(amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be less than zero");
        if(string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentNullException(nameof(clientSecret), "Client secret cannot be empty");
        
        return new PaymentRecord
        {
            OrderId = orderId,
            Provider = provider,
            IntentId = intentId,
            Status = paymentStatus,
            Amount = amount,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
            ClientSecret = clientSecret
        };
    }
    public int OrderId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string IntentId { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public string ClientSecret { get; private set; } = null!;
    
    public void ChangePaymentStatus(PaymentStatus newStatus)
    {
        if(Status == PaymentStatus.Approved) throw new InvalidOperationException("Payment already made.");
        if(Status == PaymentStatus.Failed)  throw new InvalidOperationException("Payment was declined.");
        
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}