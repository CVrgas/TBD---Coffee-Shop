using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Inventory.Dtos;
using Application.Inventory.Specifications;
using Domain.Inventory;
using MediatR;

namespace Application.Inventory.Commands.GetStockLevel;

public class GetStockLevelCommandHandler(IRepository<StockItem, int> repository) : IRequestHandler<GetStockLevelCommand, Envelope<StockLevelDto>>
{
    public async Task<Envelope<StockLevelDto>> Handle(GetStockLevelCommand request, CancellationToken cancellationToken)
    {
        var stockItems = await repository.ListAsync(new StockLevelByProductIdSpec(request.ProductId), ct: cancellationToken);

        var stockLevel = new StockLevelDto
        {
            ProductId = request.ProductId,
            QuantityInStock = stockItems.Sum(s => s.QuantityOnHand),
            ReservedQuantity = stockItems.Sum(s => s.ReservedQuantity),
            AvailableQuantity = stockItems.Sum(s => s.AvailableQuantity),
            LocationStockLevels = stockItems.GroupBy(s => s.LocationId).Select(g => new LocationStockLevelDto
            {
                LocationId = g.Key,
                AvailableQuantity = g.Sum(s => s.AvailableQuantity)
            }).ToList()
        };
        
        return Envelope<StockLevelDto>.Ok(stockLevel);
    }
}