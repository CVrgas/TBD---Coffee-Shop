using Domain.Base;
using Domain.Catalog;

namespace Domain.Inventory;

public class StockItem : EntityWithRowVersion<int>
{
    public int ProductId { get; set; }
    public int LocationId { get; set; } 
    public int QuantityOnHand { get; private set; } // Current quantity
    public int ReorderLevel { get; private set; } // when to reorder
    public int ReservedQuantity { get; private set; } // quantity reserved
    public bool IsActive { get; set; } 
    public DateTime LastMovementAt  { get; set; }
    public Product Product { get; set; } = null!;
    public IEnumerable<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public void ReserveStock(int quantity)
    {
        if(QuantityOnHand < quantity)
            throw new InvalidOperationException("Cannot reserve more than stock.");
        
        QuantityOnHand -= quantity;
        ReservedQuantity += quantity;
    }

    public void AdjustQuantity(int newQty)
    {
        if(newQty < 0)
            throw new InvalidOperationException("Cannot adjust quantity of negative.");
        
        QuantityOnHand = newQty;
    }

    public void RestoreStock(int quantity)
    {
        if(ReservedQuantity < quantity)
            throw new InvalidOperationException("Cannot restore more than reserved stock.");
        
        QuantityOnHand += quantity;
        ReservedQuantity -= quantity;
    }
}