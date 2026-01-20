using Application.Orders.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.Orders;

namespace Application.Orders.Mapping;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order o)
    {
        return new OrderDto(
            o.OrderNumber, 
            o.Status.ToString(), 
            o.Subtotal, 
            o.Tax, 
            o.Total, 
            o.Currency.Code,
            o.CreatedAt,
            o.UpdatedAt,
            o.OrderItems.Select(oi => new OrderItemDto(oi.ProductId, oi.Qty)).ToList());
    }
}