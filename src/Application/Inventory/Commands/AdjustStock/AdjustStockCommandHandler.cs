using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Inventory.Commands.AdjustStock;

public class AdjustStockCommandHandler(IAppDbContext context) : IRequestHandler<AdjustStockCommand, Envelope>
{
    public async Task<Envelope> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var item = await context.StockItems.FirstOrDefaultAsync(s => s.IsActive && s.ProductId == request.ProductId, cancellationToken: cancellationToken);
        if(item == null) return Envelope.NotFound("No stock for this item found");

        item.AdjustStock(request.Delta, reference: request.ReferenceId);
            
        await context.SaveChangesAsync(cancellationToken);
        return Envelope.Ok();
    }
}