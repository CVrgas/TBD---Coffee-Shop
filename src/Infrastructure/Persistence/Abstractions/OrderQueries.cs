using System.Linq.Expressions;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Common.Interfaces.User;
using Application.Orders.Dtos;
using Application.Orders.Services;
using Domain.Orders;
using Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Abstractions;

public class OrderQueries(ApplicationDbContext context, ICurrentUserService userService) : IOrderQueries
{
    public async Task<OrderDto?> GetByUserIdAsync(int? userId, CancellationToken cancellationToken = default)
    {
        var queryId = userService.UserRole == UserRole.Admin && userId.HasValue
            ? userId // admin can search other users Orders.
            : userService.RequiredUserId;
        
        return await context.Orders.AsNoTracking()
            .Where(o => o.UserId == queryId)
            .Select(_getOrderProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<OrderDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(_getOrderProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Paginated<OrderDto>> PaginatedAsync(OrderPaginatedRequest request, CancellationToken cancellationToken = default)
    {
        var queryId = userService.UserRole == UserRole.Admin && request.UserId.HasValue
            ? request.UserId.Value // admin can search other users Orders.
            : userService.RequiredUserId;
        
        var queryable = context.Orders.AsNoTracking()
            .Where(o => o.UserId == queryId &&
                (string.IsNullOrWhiteSpace(request.QueryPattern) || o.OrderNumber.Contains(request.QueryPattern)))
            .ApplySort(request.SortOption);
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        queryable = queryable.Skip(request.Skip).Take(request.PageSize);
        
        var orders = await queryable.Select(_getOrderProjection).ToListAsync(cancellationToken);
        return new Paginated<OrderDto>(orders, totalCount, request.PageIndex, request.PageSize);
    }
    
    private readonly Expression<Func<Order, OrderDto>> _getOrderProjection = o => new OrderDto(
        o.OrderNumber,
        o.Status.ToString(),
        o.Subtotal,
        o.Tax,
        o.Total,
        o.Currency.Code,
        o.CreatedAt,
        o.UpdatedAt,
        o.OrderItems.Select( oi => new OrderItemDto(oi.ProductId, oi.Qty)).ToList()
    );
}