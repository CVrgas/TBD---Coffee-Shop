namespace Application.Inventory.Dtos;

public record StockItemDto(int ProductId, decimal QuantityOnHand, decimal ReservedQuantity, bool IsActive, DateTime LastMovementAt, byte[] RowVersion);