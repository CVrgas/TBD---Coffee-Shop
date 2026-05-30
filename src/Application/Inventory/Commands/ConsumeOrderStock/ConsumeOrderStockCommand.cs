using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Inventory.Commands.ConsumeOrderStock;

public sealed record ConsumeOrderStockCommand(string OrderId, Dictionary<int, int> Movements) : IRequest<Envelope>;

public class ConsumeOrderStockCommandHandler(IAppDbContext context) : IRequestHandler<ConsumeOrderStockCommand, Envelope>
{
    public async Task<Envelope> Handle(ConsumeOrderStockCommand request, CancellationToken cancellationToken)
    {
        var productsIds = request.Movements.Keys.ToHashSet();
        
        var stockItems = await context.StockItems
            .Where(si => productsIds.Contains(si.ProductId))
            .ToListAsync(cancellationToken);
        
        if (stockItems.Count == 0)
            return Envelope.NotFound("Stock item not found.");

        
        foreach (var items in stockItems)
        {
            items.ConsumeReservedStock(request.Movements[items.ProductId], orderId: request.OrderId);
        }
        
        await context.SaveChangesAsync(cancellationToken);

        return Envelope.Ok();
    }
}