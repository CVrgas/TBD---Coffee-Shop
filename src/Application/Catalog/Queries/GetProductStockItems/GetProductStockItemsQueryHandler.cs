using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Inventory.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetProductStockItems;

public class GetProductStockItemsQueryHandler(IAppDbContext context) : IRequestHandler<GetProductStockItemsQuery, Envelope<List<StockItemDto>>>
{
    public async Task<Envelope<List<StockItemDto>>> Handle(GetProductStockItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await context.StockItems
            .AsNoTracking()
            .Where(s => s.ProductId == request.ProductId)
            .Select(item => new StockItemDto(item.ProductId, item.QuantityOnHand, item.ReservedQuantity, item.IsActive, item.RowVersion))
            .ToListAsync(cancellationToken: cancellationToken);
            
        return Envelope<List<StockItemDto>>.Ok(result);
    }
}
