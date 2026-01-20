using Application.Common.Abstractions.Persistence.Repository;
using Application.Inventory.Abstractions;
using Domain.Base;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Abstractions;

public class InventoryRepository(IRepository<StockItem, int> repository, IRepository<StockMovement, int> movementRepository) : IInventoryRepository
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
        var stockItems = await repository.ListAsync(
            predicate: s => movements.Keys.Contains(s.ProductId) 
                 && s.IsActive 
                 && s.Product.IsActive 
                 && s.QuantityOnHand > 0,
            asNoTracking: false,
            ct: ct);

        var listedItems = stockItems.ToList();
        
        if (!RequiredQtyLookup(listedItems, movements, out var adjustmentNeeded)) return false;

        foreach (var (stockId, quantity) in adjustmentNeeded)
        {
            var item = listedItems.First(s => s.Id == stockId);
            
            item.ReserveStock(quantity);
            
            await movementRepository.Create(
                new StockMovement {
                    ProductId = item.ProductId,
                    CreatedBy = userId,
                    Reason = StockMovementReason.Reserve,
                    Delta = quantity,
                    StockItemId = item.Id,
                    ReferenceId = referenceId 
                });
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
        var stockItems = await repository.ListAsync(
            i => i.StockMovements.Any(m => m.ReferenceId == orderId),
            includes: q => q.Include(i =>
                i.StockMovements.Where(mv => mv.ReferenceId == orderId)),
            asNoTracking: false,
            ct: ct);

        foreach (var stockItem in stockItems)
        {
            var orderMovements = stockItem.StockMovements.Where(mv => mv.ReferenceId == orderId).ToList();
            
            var reserved = orderMovements.Where(mv => mv.Reason == StockMovementReason.Reserve)
                .Sum(mv => mv.Delta);

            var alreadyRestored = orderMovements.Where(mv => mv.Reason == StockMovementReason.Restore)
                .Sum(mv => mv.Delta);

            var pendingRestore = reserved - alreadyRestored;
            
            if (pendingRestore <= 0) continue;
            
            stockItem.RestoreStock(pendingRestore);
            
            await movementRepository.Create(new StockMovement {
                ProductId = stockItem.ProductId,
                CreatedBy = userId,
                Reason = StockMovementReason.Restore,
                Delta = pendingRestore,
                StockItemId = stockItem.Id,
                ReferenceId = orderId
            });
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
                var take = Math.Min(needed, item.QuantityOnHand);
                current += take;
                response[item.Id] = take;
            }

            if (current < requiredQty) return false;
        }
        
        proposedMovements = response;
        return true;
    }
    

}