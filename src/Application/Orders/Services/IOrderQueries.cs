using Application.Common.Abstractions.Persistence;
using Application.Orders.Dtos;

namespace Application.Orders.Services;

public interface IOrderQueries
{
    Task<OrderDto?> GetByUserIdAsync(int? userId, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Paginated<OrderDto>> PaginatedAsync(OrderPaginatedRequest request, CancellationToken cancellationToken = default);
}