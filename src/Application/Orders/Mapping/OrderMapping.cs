using Application.Orders.Dtos;
using Domain.Orders;

namespace Application.Orders.Mapping;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order o, IEnumerable<OrderItemDto>? items = null)
    {
        return new OrderDto(
            OrderNumber: o.OrderNumber,
            Status: o.Status.ToString(),
            Subtotal: o.Subtotal,
            Tax: o.Tax,
            Total: o.Total,
            Currency: o.Currency.Code,
            CreatedAt: o.CreatedAt.UtcDateTime,
            UpdatedAt: o.UpdatedAt.UtcDateTime,
            OrderItems: items ?? new List<OrderItemDto>()
        );
    }
}