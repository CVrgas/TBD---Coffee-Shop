using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.User;
using Application.Inventory.Abstractions;
using Application.Orders.Specifications;
using Domain.Base;
using Domain.Orders;
using Domain.User;
using MediatR;

namespace Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler(
    IRepository<Order, int> repository,
    IUnitOfWork uOw,
    IInventoryRepository invRepository,
    ICurrentUserService userContext) 
    : IRequestHandler<CancelOrderCommand, Envelope>
{
    private int CurrentUserId => userContext.RequiredUserId;
    
    public async Task<Envelope> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var order = await repository.GetAsync(new GetOrderCancelSpec(request.OrderNumber), asNoTracking: false, ct: cancellationToken);

            if (order is null) return Envelope.NotFound();
            if (order.UserId != CurrentUserId && userContext.UserRole != UserRole.Admin) return Envelope.Forbidden();

            order.UpdateStatus(OrderStatus.Cancelled);
            await invRepository.RestoreStock(order.OrderNumber, CurrentUserId.ToString(), cancellationToken);
            return Envelope.Ok();
        }, cancellationToken);
    }
}