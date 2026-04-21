using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Interfaces;
using Application.Common.Interfaces.User;
using Application.Orders.Dtos;
using Domain.Users.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Queries.GetPaginatedOrder;

public class GetPaginatedOrderHandler(IAppDbContext context, ICurrentUserService userService) : IRequestHandler<GetPaginatedOrderCommand, Envelope<Paginated<OrderDto>>>
{
    public async Task<Envelope<Paginated<OrderDto>>> Handle(GetPaginatedOrderCommand request, CancellationToken cancellationToken)
    {
        var queryId = userService.UserRole == UserRole.Admin && request.UserId.HasValue
            ? request.UserId.Value
            : userService.RequiredUserId;

        var queryable = context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == queryId &&
                        (string.IsNullOrWhiteSpace(request.QueryPattern) ||
                         o.OrderNumber.Contains(request.QueryPattern)));
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        // TODO: Apply order
        
        var orders = await queryable
            .Skip(request.Skip)
            .Take(request.PageSize!.Value)
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
                )
            ).ToListAsync(cancellationToken);
        
        return Envelope<Paginated<OrderDto>>.Ok(request.ComposeResponse(orders, totalCount));
    }
}