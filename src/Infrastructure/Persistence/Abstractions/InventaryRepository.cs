using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Application.Inventory.Abstractions;
using Domain.Base;
using Domain.Inventory;

namespace Infrastructure.Persistence.Abstractions;

public class InventoryRepository(IRepository<StockItem, int> repository) : IInventoryRepository
{
    /// <summary>
    /// Reserve product for a user.
    /// </summary>
    /// <param name="movements"> Product id + Wanted Quantity</param>
    /// <param name="userId">Created by user</param>
    /// <param name="referenceId">Optional reference</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns> True for success and False when not enough stock</returns>
    public async Task<bool> ReserveStock(IDictionary<int, int> movements, string userId, string referenceId, CancellationToken ct = default)
    {
        var stockItems = await repository.ListAsync(new StockByProductIdsSpec(movements.Keys.ToList()), asNoTracking: false, ct: ct);
        
        if (!RequiredQtyLookup(stockItems, movements, out var adjustmentNeeded)) return false;

        foreach (var (stockId, quantity) in adjustmentNeeded)
        {
            var item = stockItems.First(s => s.Id == stockId);
            item.ReserveStock(quantity, referenceId);
        }

        return true;
    }
    
    /// <summary>
    /// Restore product for a user.
    /// </summary>
    /// <param name="orderId">Order that reserved the stock</param>
    /// <param name="userId"></param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns> True for success and False when not enough stock</returns>
    public async Task<bool> RestoreStock(string orderId, string userId, CancellationToken ct = default)
    {
        var stockItems = await repository.ListAsync(new StockByReference(orderId), asNoTracking: false, ct: ct);
        foreach (var stockItem in stockItems)
        {
            var orderMovements = stockItem.Movements.Where(mv => mv.ReferenceId == orderId).ToList();
            
            var reserved = orderMovements.Where(mv => mv.Reason == StockMovementReason.Reserve)
                .Sum(mv => mv.Delta);

            var alreadyRestored = orderMovements.Where(mv => mv.Reason == StockMovementReason.Restore)
                .Sum(mv => mv.Delta);

            var pendingRestore = reserved - alreadyRestored;
            
            if (pendingRestore <= 0) continue;
            
            stockItem.ReleaseReservation(pendingRestore, orderId);
        }
        return true;
    }

    private static bool RequiredQtyLookup(
        IList<StockItem> stockItems, 
        IDictionary<int, int> requiredQtyLookup, 
        out Dictionary<int, int> proposedMovements)
    {
        var stockLookup = stockItems.ToLookup(s => s.ProductId);
        var response = new Dictionary<int, int>();
        proposedMovements = new Dictionary<int, int>();

        foreach (var (productId, requiredQty) in requiredQtyLookup)
        {
            var available = stockLookup[productId];
            var current = 0;
            
            foreach (var item in available)
            {
                if (current >= requiredQty) break;
                
                var needed = requiredQty - current;
                var take = Math.Min(needed, item.AvailableQuantity);
                current += take;
                response[item.Id] = take;
            }

            if (current < requiredQty) return false;
        }
        
        proposedMovements = response;
        return true;
    }
    

}

internal class StockByProductIdsSpec(List<int> productsIds)
    : Specification<StockItem>(s => (!productsIds.Any() || productsIds.Contains(s.ProductId)) && s.IsActive && s.QuantityOnHand > s.ReservedQuantity);

internal class StockByReference : Specification<StockItem>
{
    public StockByReference(string referenceId) : base(s => s.Movements.Any(m => m.ReferenceId == referenceId))
    {
        AddInclude(s => s.Movements.Where(m => m.ReferenceId == referenceId));
    }
}