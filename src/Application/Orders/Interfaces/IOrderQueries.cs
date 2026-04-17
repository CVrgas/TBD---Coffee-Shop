using Application.Common.Abstractions.Persistence;
using Application.Orders.Commands.GetPaginatedOrder;
using Application.Orders.Dtos;

namespace Application.Orders.Interfaces;

public interface IOrderQueries
{
    Task<OrderDto?> GetByUserIdAsync(int? userId, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Paginated<OrderDto>> PaginatedAsync(GetPaginatedOrderCommand request, CancellationToken cancellationToken = default);
}