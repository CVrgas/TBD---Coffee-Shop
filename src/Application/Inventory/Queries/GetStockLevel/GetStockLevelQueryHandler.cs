using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Inventory.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Inventory.Queries.GetStockLevel;

public class GetStockLevelQueryHandler(IAppDbContext context) : IRequestHandler<GetStockLevelQuery, Envelope<StockLevelDto>>
{
    public async Task<Envelope<StockLevelDto>> Handle(GetStockLevelQuery request, CancellationToken cancellationToken)
    {
        
        var stockItems = await context.StockItems
            .AsNoTracking()
            .Where(s => s.IsActive && s.ProductId == request.ProductId)
            .GroupBy(s => 1)
            .Select(stockItems => new StockLevelDto
            {
                ProductId = request.ProductId,
                QuantityInStock = stockItems.Sum(s => s.QuantityOnHand),
                ReservedQuantity = stockItems.Sum(s => s.ReservedQuantity),
                LocationStockLevels = stockItems.GroupBy(s => s.LocationId).Select(g => new LocationStockLevelDto
                {
                    LocationId = g.Key,
                    AvailableQuantity = g.Sum(s => s.QuantityOnHand) - g.Sum(s => s.ReservedQuantity)
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        return stockItems is not null ?  Envelope<StockLevelDto>.Ok(stockItems) : Envelope<StockLevelDto>.NotFound("Not stock found for this product.");
    }
}