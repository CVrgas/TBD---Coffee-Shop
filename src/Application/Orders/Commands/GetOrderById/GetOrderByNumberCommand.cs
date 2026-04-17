using Application.Common.Abstractions.Envelope;
using Application.Orders.Dtos;
using Application.Orders.Interfaces;
using MediatR;

namespace Application.Orders.Commands.GetOrderById;

public sealed record GetOrderByNumberCommand(string OrderNumber) : IRequest<Envelope<OrderDto>>;

public class GetOderByNumberCommandHandler(IOrderQueries queries) : IRequestHandler<GetOrderByNumberCommand, Envelope<OrderDto>>
{
    public async Task<Envelope<OrderDto>> Handle(GetOrderByNumberCommand request, CancellationToken cancellationToken)
    {
        var order = await queries.GetByNumberAsync(request.OrderNumber, cancellationToken);
        return order is null ? Envelope<OrderDto>.NotFound() : Envelope<OrderDto>.Ok(order);
    }
}