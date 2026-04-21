using Domain.Base.Entities;

namespace Domain.Orders.Entities;

public class OrderItem : Entity<int>
{
    private OrderItem(){}

    public static OrderItem Create(int productId, string name, decimal price, int quantity)
    {
        if(quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        
        var item = new OrderItem
        {
            ProductId = productId,
            NameSnapshot = name,
            UnitPrice = price,
            Quantity = quantity,
        };
        item.CalculateLineTotal();
        return item;
    }
    
    public int OrderId { get; private set;}  
    public int ProductId { get; private set;} 
    public string NameSnapshot { get; private set;} = null!;
    public decimal UnitPrice { get; private set;}
    public int Quantity { get; private set;} 
    public decimal LineTotal { get; private set;}
    public Order Order { get; private set; } = null!;

    internal void AddQuantity(int quantity)
    {
        if(quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        
        Quantity += quantity;
        CalculateLineTotal();
    }
    private void CalculateLineTotal()
    {
        LineTotal = UnitPrice * Quantity;
    }
}