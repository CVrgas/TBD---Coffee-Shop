using Domain.Base.ValueObjects;
using Domain.Catalog;
using Domain.Orders.Entities;

namespace Unit.Order;

public class OrderItemUnitTests
{
    // Create
    [Fact]
    public void Create_WithValidQuantity_CalculatesLineTotalCorrectly()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        const int quantity = 2;

        // Act
        var orderItem = OrderItem.Create(product.Id, product.Name, product.Price, quantity);

        // Assert
        Assert.Equal(product.Price * quantity, orderItem.LineTotal);
    }

    [Fact]
    public void Create_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        const int quantity = 0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderItem.Create(product.Id, product.Name, product.Price, quantity));
    }

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var quantity = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderItem.Create(product.Id, product.Name, product.Price, quantity));
    }
    
    // AddQuantity
    [Fact]
    public void AddQuantity_WithValidPositiveQuantity_IncreasesQuantityAndRecalculatesLineTotal()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var orderItem = OrderItem.Create(product.Id, product.Name, product.Price, 2);
        var quantityToAdd = 3;
        var expectedQuantity = 5;
        var expectedLineTotal = product.Price * expectedQuantity;

        // Act
        orderItem.AddQuantity(quantityToAdd);

        // Assert
        Assert.Equal(expectedQuantity, orderItem.Quantity);
        Assert.Equal(expectedLineTotal, orderItem.LineTotal);
    }

    [Fact]
    public void AddQuantity_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var orderItem = OrderItem.Create(product.Id, product.Name, product.Price, 2);
        var quantityToAdd = 0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => orderItem.AddQuantity(quantityToAdd));
    }

    [Fact]
    public void AddQuantity_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var orderItem = OrderItem.Create(product.Id, product.Name, product.Price, 2);
        var quantityToAdd = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => orderItem.AddQuantity(quantityToAdd));
    }
}