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

public class AdjustStockCommandHandler(IRepository<StockItem, int> stockRepository, IUnitOfWork uOw) 
    : IRequestHandler<AdjustStockCommand, Envelope>
{
    public async Task<Envelope> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var item = await stockRepository.GetAsync(new AdjustSpec(request.ProductId), asNoTracking: false, ct: cancellationToken);
        if(item == null) return Envelope.NotFound("No stock for this item found");

        item.AdjustStock(request.Delta, reference: request.ReferenceId);
            
        await uOw.SaveChangesAsync(cancellationToken);
        return Envelope.Ok();
    }
}