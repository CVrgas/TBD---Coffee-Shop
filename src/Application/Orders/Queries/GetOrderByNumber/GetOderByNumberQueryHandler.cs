using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Orders.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Queries.GetOrderByNumber;

public class GetOderByNumberQueryHandler(IAppDbContext context) : IRequestHandler<GetOrderByNumberQuery, Envelope<OrderDto>>
{
    public async Task<Envelope<OrderDto>> Handle(GetOrderByNumberQuery request, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Where(o => o.OrderNumber == request.OrderNumber)
            .Select(o => new OrderDto(
                o.OrderNumber,
                o.Status.ToString(),
                o.Subtotal,
                o.Tax,
                o.Total,
                o.Currency.Code,
                o.CreatedAt.UtcDateTime,
                o.UpdatedAt.HasValue ? o.UpdatedAt.Value.UtcDateTime : null,
                o.OrderItems.Select( oi => new OrderItemDto(oi.ProductId, oi.Quantity)).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
        return order is null ? Envelope<OrderDto>.NotFound() : Envelope<OrderDto>.Ok(order);
    }
}