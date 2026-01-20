using Domain.Base;
using Domain.Inventory;

namespace Application.Inventory.Dtos;


public sealed record AdjustStockDto(
    int ProductId, 
    int Delta, 
    StockMovementReason Reason, 
    byte[] RowVersion, 
    string? ReferenceId);