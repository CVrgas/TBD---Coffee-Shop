using Domain.Base;

namespace Domain.Inventory;

public sealed class StockMovement : Entity<int>
{
    internal StockMovement(int quantityDelta, int reservedDelta, StockMovementReason reason, string? referenceId)
    {
        QuantityDelta = quantityDelta;
        ReservedDelta = reservedDelta;
        Reason = reason;
        ReferenceId = referenceId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? ReferenceId { get; private set; }
    public StockMovementReason Reason { get; private set; }
    public int QuantityDelta { get; private set; }
    public int ReservedDelta { get; private set; }
    
    public int? LocationId { get; private set; }
    public int StockItemId { get; private set; }
    public StockItem StockItem { get; private set; } = null!;
}