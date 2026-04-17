using Domain.Base;
using Domain.Catalog;

namespace Domain.Orders;

public class OrderItem : Entity<int>
{
    private OrderItem(){}
    private OrderItem(Order order, Product product, int quantity)
    {
        if(quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        
        Order = order;
        ProductId = product.Id;
        NameSnapshot = product.Name;
        UnitPrice = product.Price;
        Quantity = quantity;
        CalculateLineTotal();
    }

    public static OrderItem Create(Order order, Product product, int quantity)
    {
        return new OrderItem(order, product, quantity);
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