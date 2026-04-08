using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.GetPaginatedOrder;

public class GetPaginatedOrderCommand : PaginatedRequestBase, IRequest<Envelope<Paginated<OrderDto>>>
{
    public int? UserId { get; set; }
    private string? _query;
    public string? Query { get => _query; set => _query = value?.Trim(); }
    public string? QueryPattern => !string.IsNullOrEmpty(Query) 
        ? $"%{Utilities.EscapeLike(Query)}%" 
        : null;
}