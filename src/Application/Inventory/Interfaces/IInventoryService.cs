using Application.Common.Abstractions.Envelope;
using Application.Inventory.Dtos;

namespace Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task<Envelope<List<StockItemDto>>> GetStockItemsAsync(int productId, CancellationToken cancellationToken = default);
    Task<Envelope> AdjustStock(AdjustStockDto adjustStockDto, CancellationToken cancellationToken = default);
}