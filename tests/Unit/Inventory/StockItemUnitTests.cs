using Domain.Base;
using Domain.Inventory;

namespace Unit.Inventory;

public class StockItemUnitTests
{
    [Fact]
    public void Initialize_WithValidData_SetsCorrectInitialState()
    {
        var item = StockItem.Initialize(100, 2);

        Assert.Equal(100, item.ProductId);
        Assert.Equal(2, item.LocationId);
        Assert.Equal(0, item.QuantityOnHand);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(0, item.AvailableQuantity);
        Assert.True(item.IsActive);
        Assert.Empty(item.Movements);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Initialize_WithInvalidProductId_ThrowsArgumentException(int productId)
    {
        Assert.Throws<ArgumentException>(() => StockItem.Initialize(productId));
    }

    [Fact]
    public void ReceiveStock_WithValidQuantity_UpdatesQuantityAndRecordsMovement()
    {
        var item = StockItem.Initialize(1);
        
        item.ReceiveStock(50, StockMovementReason.PurchaseOrder, "PO-123");

        Assert.Equal(50, item.QuantityOnHand);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(50, item.AvailableQuantity);
        
        var movement = Assert.Single(item.Movements);
        Assert.Equal(50, movement.QuantityDelta);
        Assert.Equal(0, movement.ReservedDelta);
        Assert.Equal(StockMovementReason.PurchaseOrder, movement.Reason);
        Assert.Equal("PO-123", movement.ReferenceId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ReceiveStock_WithInvalidQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        var item = StockItem.Initialize(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => item.ReceiveStock(quantity));
    }

    [Fact]
    public void ReserveStock_WithSufficientAvailability_UpdatesReservedAndRecordsMovement()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        
        item.ReserveStock(20, "ORD-1");

        Assert.Equal(50, item.QuantityOnHand);
        Assert.Equal(20, item.ReservedQuantity);
        Assert.Equal(30, item.AvailableQuantity);

        var movement = item.Movements.Last();
        Assert.Equal(0, movement.QuantityDelta);
        Assert.Equal(20, movement.ReservedDelta);
        Assert.Equal(StockMovementReason.Reserve, movement.Reason);
        Assert.Equal("ORD-1", movement.ReferenceId);
    }

    [Fact]
    public void ReserveStock_WithInsufficientAvailability_ThrowsInvalidOperationException()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(10);
        
        Assert.Throws<InvalidOperationException>(() => item.ReserveStock(15, "ORD-1"));
    }

    [Fact]
    public void ConsumeReservedStock_WithValidQuantity_DecreasesBothAndRecordsMovement()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        item.ReserveStock(20, "ORD-1");
        
        item.ConsumeReservedStock(15, "INV-1");

        Assert.Equal(35, item.QuantityOnHand);
        Assert.Equal(5, item.ReservedQuantity);
        Assert.Equal(30, item.AvailableQuantity);

        var movement = item.Movements.Last();
        Assert.Equal(-15, movement.QuantityDelta);
        Assert.Equal(-15, movement.ReservedDelta);
        Assert.Equal(StockMovementReason.PurchaseOrder, movement.Reason);
        Assert.Equal("INV-1", movement.ReferenceId);
    }

    [Fact]
    public void ConsumeReservedStock_ExceedingReservedQuantity_ThrowsInvalidOperationException()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        item.ReserveStock(20, "ORD-1");
        
        Assert.Throws<InvalidOperationException>(() => item.ConsumeReservedStock(25, "INV-1"));
    }

    [Fact]
    public void ReleaseReservation_WithValidQuantity_DecreasesReservedAndRecordsMovement()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        item.ReserveStock(20, "ORD-1");
        
        item.ReleaseReservation(10, "CANC-1");

        Assert.Equal(50, item.QuantityOnHand);
        Assert.Equal(10, item.ReservedQuantity);
        Assert.Equal(40, item.AvailableQuantity);

        var movement = item.Movements.Last();
        Assert.Equal(0, movement.QuantityDelta);
        Assert.Equal(-10, movement.ReservedDelta);
        Assert.Equal(StockMovementReason.Restore, movement.Reason);
        Assert.Equal("CANC-1", movement.ReferenceId);
    }

    [Fact]
    public void ReleaseReservation_ExceedingReservedQuantity_ThrowsInvalidOperationException()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        item.ReserveStock(20, "ORD-1");
        
        Assert.Throws<InvalidOperationException>(() => item.ReleaseReservation(25, "CANC-1"));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    public void AdjustStock_WithValidDelta_UpdatesQuantityAndRecordsMovement(int delta)
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        
        item.AdjustStock(delta, StockMovementReason.Adjustment, "ADJ-1");

        Assert.Equal(50 + delta, item.QuantityOnHand);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(50 + delta, item.AvailableQuantity);

        var movement = item.Movements.Last();
        Assert.Equal(delta, movement.QuantityDelta);
        Assert.Equal(0, movement.ReservedDelta);
        Assert.Equal(StockMovementReason.Adjustment, movement.Reason);
        Assert.Equal("ADJ-1", movement.ReferenceId);
    }

    [Fact]
    public void AdjustStock_WithZeroDelta_ThrowsArgumentException()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        
        Assert.Throws<ArgumentException>(() => item.AdjustStock(0));
    }

    [Fact]
    public void AdjustStock_CausingNegativeQuantity_ThrowsInvalidOperationException()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(10);
        
        Assert.Throws<InvalidOperationException>(() => item.AdjustStock(-15));
    }

    [Fact]
    public void AdjustStock_CausingQuantityBelowReserved_ThrowsInvalidOperationException()
    {
        var item = StockItem.Initialize(1);
        item.ReceiveStock(50);
        item.ReserveStock(40, "ORD-1");
        
        Assert.Throws<InvalidOperationException>(() => item.AdjustStock(-15));
    }
}