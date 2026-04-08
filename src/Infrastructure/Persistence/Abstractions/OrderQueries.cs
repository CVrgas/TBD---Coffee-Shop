using System.Linq.Expressions;
using Application.Common.Abstractions.Persistence;
using Application.Common.Interfaces.User;
using Application.Orders.Commands.GetPaginatedOrder;
using Application.Orders.Dtos;
using Application.Orders.Interfaces;
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
    
    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(_getOrderProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<OrderDto?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await context.Orders.AsNoTracking()
            .Where(o => o.OrderNumber == orderNumber)
            .Select(_getOrderProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Paginated<OrderDto>> PaginatedAsync(GetPaginatedOrderCommand request, CancellationToken cancellationToken = default)
    {
        var queryId = userService.UserRole == UserRole.Admin && request.UserId.HasValue
            ? request.UserId.Value // admin can search other users Orders.
            : userService.RequiredUserId;
        
        var queryable = context.Orders.AsNoTracking()
            .Where(o => o.UserId == queryId &&
                (string.IsNullOrWhiteSpace(request.QueryPattern) || o.OrderNumber.Contains(request.QueryPattern)))
            .ApplySort(request.SortOption);
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        queryable = queryable.Skip(request.Skip).Take(request.PageSize!.Value);
        
        var orders = await queryable.Select(_getOrderProjection).ToListAsync(cancellationToken);
        return new Paginated<OrderDto>(orders, totalCount, request.PageIndex!.Value, request.PageSize.Value);
    }
    
    private readonly Expression<Func<Order, OrderDto>> _getOrderProjection = o => new OrderDto(
        o.OrderNumber,
        o.Status.ToString(),
        o.Subtotal,
        o.Tax,
        o.Total,
        o.Currency.Code,
        o.CreatedAt.UtcDateTime,
        o.UpdatedAt.UtcDateTime,
        o.OrderItems.Select( oi => new OrderItemDto(oi.ProductId, oi.Quantity)).ToList()
    );
}