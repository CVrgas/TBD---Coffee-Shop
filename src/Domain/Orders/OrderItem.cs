using Domain.Base;
using Domain.Catalog;

namespace Domain.Orders;

public class OrderItem : Entity<int>
{
    private OrderItem(){}
    public OrderItem(Order order, Product product, int quantity)
    {
        Order = order;
        ProductId = product.Id;
        NameSnapshot = product.Name;
        UnitPrice = product.Price;
        Quantity = quantity;
        CalculateLineTotal();
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
        Quantity += quantity;
        CalculateLineTotal();
    }
    private void CalculateLineTotal()
    {
        LineTotal = UnitPrice * Quantity;
    }
}