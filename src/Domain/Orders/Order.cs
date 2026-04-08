using Domain.Base;
using Domain.Catalog;

namespace Domain.Orders;

public class Order : EntityWithRowVersion<int>
{
    private Order(){}
    
    public static Order Create(int userId, CurrencyCode currency, decimal taxPercentage, List<OrderItem>? orderItems = null)
    {
        return new Order
        {
            UserId = userId,
            Currency = currency,
            Status = OrderStatus.Pending,
            TaxPercentage = taxPercentage,
            CreatedAt = DateTime.UtcNow,
            OrderNumber = $"{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString()[..8].ToUpper()}"
        };
    }
    
    public string OrderNumber { get; private set; } = null!;
    public int UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal TaxPercentage { get; private set; }
    public decimal Total { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; }
    private readonly List<OrderItem> _orderItems = [];
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public void AddItem(Product product, int quantity)
    {
        if(Status != OrderStatus.Pending)
            throw new InvalidOperationException("Can only add items to pending orders.");
        
        var existingItem = _orderItems.SingleOrDefault(i => i.ProductId == product.Id);

        if (existingItem is not null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            var newItem = new OrderItem(this, product, quantity);
            _orderItems.Add(newItem);
        }

        RecalculateTotals();
    }
    public void RemoveItem(int productId)
    {
        if(Status is not OrderStatus.Pending)
            throw new InvalidOperationException("Can't modify finalized orders.");
        
        var existingItem = _orderItems.SingleOrDefault(i => i.ProductId == productId);
        if (existingItem is null) return;
        _orderItems.Remove(existingItem);
        RecalculateTotals();
    }
    public void UpdateStatus(OrderStatus status)
    {
        if(Status == status) return;
        Status = status;
    }
    private void RecalculateTotals()
    {
        Subtotal = _orderItems.Sum(i => i.LineTotal);
        Tax = Subtotal * TaxPercentage;
        Total = Subtotal + Tax;
        UpdatedAt = DateTime.UtcNow;
    }
}