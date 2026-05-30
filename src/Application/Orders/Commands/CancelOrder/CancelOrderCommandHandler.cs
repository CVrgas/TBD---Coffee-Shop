using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Common.Interfaces.User;
using Domain.Base.Enum;
using Domain.Users.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler(IAppDbContext context, ICurrentUserService userContext) : IRequestHandler<CancelOrderCommand, Envelope>
{
    private int CurrentUserId => userContext.RequiredUserId;
    
    public async Task<Envelope> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber, cancellationToken);

            if (order is null) return Envelope.NotFound();
            if (order.UserId != CurrentUserId && userContext.UserRole != UserRole.Admin) return Envelope.Forbidden();

            order.UpdateStatus(OrderStatus.Cancelled);
            
            await RestoreStock(order.OrderNumber, CurrentUserId.ToString(), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Envelope.Ok();
        }, cancellationToken);
    }
    
    // TODO (CQRS-MIN-001): should be a handler ( handler event).
    private async Task<bool> RestoreStock(string orderId, string userId, CancellationToken ct = default)
    {
        var stockItems = await context.StockItems
            .Where(si => si.IsActive && si.Movements.Any(sm => EF.Functions.Like(sm.ReferenceId, orderId)))
            .Include(si => si.Movements.Where(sm => EF.Functions.Like(sm.ReferenceId, orderId)))
            .ToListAsync(ct);
        
        foreach (var stockItem in stockItems)
        {
            var orderMovements = stockItem.Movements.Where(mv => mv.ReferenceId == orderId).ToList();
            
            var reserved = orderMovements.Where(mv => mv.Reason == StockMovementReason.Reserve)
                .Sum(mv => mv.ReservedDelta);

            var alreadyRestored = orderMovements.Where(mv => mv.Reason == StockMovementReason.Restore)
                .Sum(mv => mv.ReservedDelta);

            var pendingRestore = reserved - alreadyRestored;
            
            if (pendingRestore <= 0) continue;
            
            stockItem.ReleaseReservation(pendingRestore, orderId);
        }
        return true;
    }
}

