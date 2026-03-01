using Application.Common.Abstractions.Envelope;
using Application.Inventory.Dtos;

namespace Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task<Envelope> AdjustStock(AdjustStockDto adjustStockDto, CancellationToken cancellationToken = default);
}