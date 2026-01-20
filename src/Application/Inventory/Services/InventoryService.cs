using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.User;
using Application.Inventory.Dtos;
using Domain.Inventory;
using Domain.User;
using Microsoft.Extensions.Logging;

namespace Application.Inventory.Services;

public class InventoryService(
    IRepository<StockItem, int> stockRepository,
    IRepository<StockMovement, int> movementRepository,
    IUnitOfWork uOw,
    ILogger<InventoryService> logger,
    ICurrentUserService userContext)
    : IInventoryService
{

    private bool UserHasPermit => !userContext.IsAuthenticated || userContext.UserRole != UserRole.Admin || userContext.UserRole != UserRole.Staff;
    
    public async Task<Envelope<List<StockItemDto>>> GetStockItemsAsync(int productId, CancellationToken ct = default)
    {
        if(!UserHasPermit) return Envelope<List<StockItemDto>>.Unauthorized();
        
        var items = await stockRepository.ListAsync<StockItemDto>(
            item => new StockItemDto(item.ProductId, item.QuantityOnHand, item.ReservedQuantity, item.IsActive, item.LastMovementAt, item.RowVersion),  
            si => si.ProductId == productId, 
        ct: ct);

        return Envelope<List<StockItemDto>>.Ok(items.ToList());
    }

    public async Task<Envelope> AdjustStock(AdjustStockDto adjustStockDto, CancellationToken ct = default)
    {
        if(!UserHasPermit) return Envelope.Unauthorized();
        
        var item = await stockRepository.GetAsync(
            si => si.IsActive && si.ProductId == adjustStockDto.ProductId,
            asNoTracking: false,
            ct: ct);
        if(item == null) return Envelope.NotFound("No stock for this item found");
            
        stockRepository.AttachWithRowVersion(item, adjustStockDto.RowVersion);

        var newQty = item.QuantityOnHand + adjustStockDto.Delta;
        if (newQty < 0)
        {
            logger.LogWarning("Stock item would go below zero. StockId: {StockItemId}, NewQuantity: {NewQuantity}",
                item.Id, newQty);
            return Envelope.BadRequest("Invalid stock quantity, stock quantity cannot be negative");
        }
            
        if(newQty <= item.ReorderLevel)
        {
            logger.LogInformation("Stock reached reorder level. StockId: {StockItemId}, ProductId: {ProductId}",
                item.Id, item.ProductId);
            // TODO: Send alert admin/staff about reorder level.
        }
            
        item.AdjustQuantity(newQty);
        item.LastMovementAt = DateTime.UtcNow;
            
        var newMovement = new StockMovement()
        {
            StockItemId = item.Id,
            ProductId = adjustStockDto.ProductId,
            Delta = adjustStockDto.Delta,
            Reason = adjustStockDto.Reason,
            ReferenceId = adjustStockDto.ReferenceId,
            CreatedBy = userContext.UserId!.Value.ToString(),
        };
            
        await movementRepository.Create(newMovement);
        await uOw.SaveChangesAsync(ct);
        return Envelope.Ok();
    }
}