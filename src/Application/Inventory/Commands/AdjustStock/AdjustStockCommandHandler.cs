using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.User;
using Application.Inventory.Specifications;
using Domain.Inventory;
using Domain.User;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Inventory.Commands.AdjustStock;

public class AdjustStockCommandHandler(
    IRepository<StockItem, int> stockRepository,
    IUnitOfWork uOw,
    ILogger<AdjustStockCommandHandler> logger,
    ICurrentUserService userContext) 
    : IRequestHandler<AdjustStockCommand, Envelope>
{
    
    private bool UserHasPermit => !userContext.IsAuthenticated || userContext.UserRole != UserRole.Admin || userContext.UserRole != UserRole.Staff;
    public async Task<Envelope> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        if(!UserHasPermit) return Envelope.Unauthorized();
        
        var item = await stockRepository.GetAsync(new AdjustSpec(request.ProductId), asNoTracking: false, ct: cancellationToken);
        if(item == null) return Envelope.NotFound("No stock for this item found");

        var newQty = item.QuantityOnHand + request.Delta;
        if (newQty < 0)
        {
            logger.LogWarning("Stock item would go below zero. StockId: {StockItemId}, NewQuantity: {NewQuantity}",
                item.Id, newQty);
            return Envelope.BadRequest("Invalid stock quantity, stock quantity cannot be negative");
        }
        
        item.AdjustStock(newQty);
            
        await uOw.SaveChangesAsync(cancellationToken);
        return Envelope.Ok();
    }
}