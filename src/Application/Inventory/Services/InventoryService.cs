using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Application.Common.Interfaces.User;
using Application.Inventory.Dtos;
using Application.Inventory.Interfaces;
using Domain.Inventory;
using Domain.User;
using Microsoft.Extensions.Logging;

namespace Application.Inventory.Services;

public class InventoryService(
    IRepository<StockItem, int> stockRepository,
    IUnitOfWork uOw,
    ILogger<InventoryService> logger,
    ICurrentUserService userContext)
    : IInventoryService
{

    private bool UserHasPermit => !userContext.IsAuthenticated || userContext.UserRole != UserRole.Admin || userContext.UserRole != UserRole.Staff;

    public async Task<Envelope> AdjustStock(AdjustStockDto adjustStockDto, CancellationToken ct = default)
    {
        if(!UserHasPermit) return Envelope.Unauthorized();
        
        var item = await stockRepository.GetAsync(new AdjustSpec(adjustStockDto.ProductId), asNoTracking: false, ct: ct);
        if(item == null) return Envelope.NotFound("No stock for this item found");

        var newQty = item.QuantityOnHand + adjustStockDto.Delta;
        if (newQty < 0)
        {
            logger.LogWarning("Stock item would go below zero. StockId: {StockItemId}, NewQuantity: {NewQuantity}",
                item.Id, newQty);
            return Envelope.BadRequest("Invalid stock quantity, stock quantity cannot be negative");
        }
        
        item.AdjustStock(newQty);
            
        await uOw.SaveChangesAsync(ct);
        return Envelope.Ok();
    }
}

internal class AdjustSpec(int productId) : Specification<StockItem>(item => item.IsActive && item.ProductId == productId);