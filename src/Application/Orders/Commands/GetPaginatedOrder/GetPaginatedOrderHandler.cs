using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Orders.Dtos;
using Application.Orders.Interfaces;
using MediatR;

namespace Application.Orders.Commands.GetPaginatedOrder;

public class GetPaginatedOrderHandler(IOrderQueries queries) : IRequestHandler<GetPaginatedOrderCommand, Envelope<Paginated<OrderDto>>>
{
    public async Task<Envelope<Paginated<OrderDto>>> Handle(GetPaginatedOrderCommand request, CancellationToken cancellationToken)
    {
        var page = await queries.PaginatedAsync(request, cancellationToken);
        return Envelope<Paginated<OrderDto>>.Ok(page);
    }
}