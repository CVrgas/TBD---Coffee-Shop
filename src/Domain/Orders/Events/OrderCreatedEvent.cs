using Domain.Base.Events;

namespace Domain.Orders.Events;

public sealed record OrderCreatedEvent(
    int OrderId,
    string OrderNumber,
    int UserId,
    decimal Total,
    DateTimeOffset CreatedAt
) : IDomainEvent;