using Application.Inventory.Dtos;

namespace Application.Inventory.Abstractions;

public interface IInventoryQueries
{
    Task<int> GetAvailableStock(int productId, CancellationToken ct = default);
    Task<Dictionary<int, int>> GetAvailableStock(IEnumerable<int> productIds, CancellationToken ct = default);
}