using Domain.Base;
using Domain.Catalog;

namespace Unit.Order;

public class OrderUnitTests
{
    [Fact]
    public void Create_WithValidData_InstantiatesOrderWithPendingStatusAndGeneratedNumber()
    {
        // Arrange & Act
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);

        // Assert
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.False(string.IsNullOrWhiteSpace(order.OrderNumber));
    }

    [Fact]
    public void Create_SetsInitialSubtotalTaxAndTotalToZero()
    {
        // Arrange & Act
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);

        // Assert
        Assert.Equal(0, order.Subtotal);
        Assert.Equal(0, order.Tax);
        Assert.Equal(0, order.Total);
    }
    
    // AddItem
    [Fact]
    public void AddItem_WhenPendingAndNewProduct_AddsItemAndRecalculatesTotals()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var product = Product.Create("Coffee", "COF-001", 10.0m, "USD", 1);
        var quantity = 2;

        // Act
        order.AddItem(product, quantity);

        // Assert
        Assert.Single(order.OrderItems);
        Assert.Equal(20.0m, order.Subtotal);
        Assert.Equal(2.0m, order.Tax);
        Assert.Equal(22.0m, order.Total);
    }

    [Fact]
    public void AddItem_WhenPendingAndExistingProduct_IncreasesQuantityAndRecalculatesTotals()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var product = Product.Create("Coffee", "COF-001", 10.0m, "USD", 1);
        order.AddItem(product, 2);

        // Act
        order.AddItem(product, 3);

        // Assert
        Assert.Single(order.OrderItems);
        Assert.Equal(5, order.OrderItems.First().Quantity);
        Assert.Equal(50.0m, order.Subtotal);
        Assert.Equal(5.0m, order.Tax);
        Assert.Equal(55.0m, order.Total);
    }

    [Fact]
    public void AddItem_WhenStatusIsNotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        order.UpdateStatus(OrderStatus.Confirmed);
        var product = Product.Create("Coffee", "COF-001", 10.0m, "USD", 1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.AddItem(product, 1));
    }
    
    // RemoveItem
    [Fact]
    public void RemoveItem_WhenPendingAndItemExists_RemovesItemAndRecalculatesTotals()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var product = Product.Create("Coffee", "COF-001", 10.0m, "USD", 1);
        order.AddItem(product, 2);

        // Act
        order.RemoveItem(product.Id);

        // Assert
        Assert.Empty(order.OrderItems);
        Assert.Equal(0, order.Subtotal);
        Assert.Equal(0, order.Tax);
        Assert.Equal(0, order.Total);
    }

    [Fact]
    public void RemoveItem_WhenPendingAndItemDoesNotExist_DoesNothing()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var product = Product.Create("Coffee", "COF-001", 10.0m, "USD", 1);
        order.AddItem(product, 2);
        var initialSubtotal = order.Subtotal;

        // Act
        order.RemoveItem(999); // Non-existent product ID

        // Assert
        Assert.Single(order.OrderItems);
        Assert.Equal(initialSubtotal, order.Subtotal);
    }

    [Fact]
    public void RemoveItem_WhenStatusIsNotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var product = Product.Create("Coffee", "COF-001", 10.0m, "USD", 1);
        order.AddItem(product, 2);
        order.UpdateStatus(OrderStatus.Confirmed);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.RemoveItem(product.Id));
    }
    
    // UpdateStatus
    [Fact]
    public void UpdateStatus_WithNewStatus_UpdatesStatusAndSetsUpdatedAt()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var newStatus = OrderStatus.Confirmed;

        // Act
        order.UpdateStatus(newStatus);

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    [Fact]
    public void UpdateStatus_WithSameStatus_DoesNotUpdateStatusOrUpdatedAt()
    {
        // Arrange
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var initialUpdatedAt = order.UpdatedAt;
        var sameStatus = OrderStatus.Pending;

        // Act
        order.UpdateStatus(sameStatus);

        // Assert
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(initialUpdatedAt, order.UpdatedAt);
    }
}