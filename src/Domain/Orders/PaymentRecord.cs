using System.Runtime.InteropServices.JavaScript;
using Domain.Base;
using Domain.Catalog;

namespace Domain.Orders;

public class PaymentRecord : Entity<int>, IHasRowVersion
{
    public int OrderId { get; set; }
    public PaymentProvider Provider { get; set; }
    public string IntentId { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public CurrencyCode Currency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; private set; } = null!;
    public Order Order { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}