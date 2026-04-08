using Domain.Base;

namespace Domain.Inventory;

public sealed class StockItem : EntityWithRowVersion<int>
{
    public int ProductId { get; private set; } 
    public int LocationId { get; private set; } 
    public int QuantityOnHand { get; private set; } 
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
    
    private readonly List<StockMovement> _movements = [];
    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }

    private StockItem() { }
    public static StockItem Initialize(int productId, int locationId = 1)
    {
        if (productId <= 0) throw new ArgumentException("Invalid product");
        return new StockItem 
        { 
            ProductId = productId, 
            LocationId = locationId,
            QuantityOnHand = 0,
            ReservedQuantity = 0,
            IsActive = true
        };
    }

    public void ReceiveStock(int quantity, StockMovementReason reason = StockMovementReason.Unspecified)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Must receive positive amount");
        QuantityOnHand += quantity;
        _movements.Add(new StockMovement(quantity, reason, null));
    }
    
    public void ReserveStock(int quantity, string referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (AvailableQuantity < quantity) 
            throw new InvalidOperationException($"Insufficient available stock. Available: {AvailableQuantity}, Requested: {quantity}");
        
        ReservedQuantity += quantity;
        _movements.Add( new StockMovement(quantity, StockMovementReason.Reserve, referenceId));
    }
    public void ConsumeReservedStock(int quantity, string orderId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (ReservedQuantity < quantity) 
            throw new InvalidOperationException("Cannot consume more than reserved");

        ReservedQuantity -= quantity;
        QuantityOnHand -= quantity;
        
        _movements.Add(new StockMovement(-quantity, StockMovementReason.Sale, orderId));
    }
    
    public void ReleaseReservation(int quantity, string referenceId)
    {
        if (ReservedQuantity < quantity) throw new InvalidOperationException("Cannot release more than reserved");
        ReservedQuantity -= quantity;
        _movements.Add( new StockMovement(quantity, StockMovementReason.Restore, referenceId));
    }
    public void AdjustStock(int delta, StockMovementReason reason = StockMovementReason.Adjustment)
    {
        if (delta == 0) throw new ArgumentException("Delta cannot be zero");
        if (QuantityOnHand + delta < 0) throw new InvalidOperationException("Adjustment leads to negative stock");

        QuantityOnHand += delta;
        _movements.Add(new StockMovement(delta, reason, "Manual Adjustment"));
    }
}