namespace Application.Inventory.Abstractions;

public interface IInventoryRepository
{
    Task<bool> ReserveStock(IDictionary<int, int> movements, string userId, string referenceId, CancellationToken ct = default);
    Task<bool> RestoreStock(string orderId, string userId, CancellationToken ct = default);
}