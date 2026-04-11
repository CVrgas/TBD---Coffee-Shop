namespace Application.Orders.Dtos;

public sealed record OrderDto(
    string OrderNumber, 
    string Status, 
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    string Currency,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<OrderItemDto> OrderItems);
    