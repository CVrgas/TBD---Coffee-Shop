using Application.Inventory.Abstractions;
using Application.Inventory.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Abstractions;

public class InventoryQueries(ApplicationDbContext context) : IInventoryQueries
{
    public async Task<int> GetAvailableStock(int productId, CancellationToken ct = default)
    {
        return await context.StockItems.AsNoTracking()
            .Where(s => s.IsActive &&
                        productId == s.ProductId &&
                        s.QuantityOnHand > s.ReservedQuantity)
            .SumAsync(s => s.QuantityOnHand -  s.ReservedQuantity, ct);
    }

    public async  Task<Dictionary<int, int>> GetAvailableStock(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        return await context.StockItems.AsNoTracking()
            .Where(s => 
                s.IsActive && 
                productIds.Contains(s.ProductId) && 
                s.QuantityOnHand > s.ReservedQuantity)
            .GroupBy(s => s.ProductId)
            .ToDictionaryAsync(s => s.Key, s => s.Sum( si => si.QuantityOnHand - si.ReservedQuantity), cancellationToken: ct);
    }
}