using System;
using Domain.Base;
using Domain.Catalog;
using Domain.Orders;
using Xunit;

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
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);

        // Act
        var orderItem = OrderItem.Create(order, product, quantity);

        // Assert
        Assert.Equal(product.Price * quantity, orderItem.LineTotal);
    }

    [Fact]
    public void Create_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        const int quantity = 0;
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderItem.Create(order, product, quantity));
    }

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var quantity = -1;
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderItem.Create(order, product, quantity));
    }
    
    // AddQuantity
    [Fact]
    public void AddQuantity_WithValidPositiveQuantity_IncreasesQuantityAndRecalculatesLineTotal()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var orderItem = OrderItem.Create(order, product, 2);
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
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var orderItem = OrderItem.Create(order, product, 2);
        var quantityToAdd = 0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => orderItem.AddQuantity(quantityToAdd));
    }

    [Fact]
    public void AddQuantity_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
        var order = Domain.Orders.Order.Create(1, new CurrencyCode("USD"), 0.1m);
        var orderItem = OrderItem.Create(order, product, 2);
        var quantityToAdd = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => orderItem.AddQuantity(quantityToAdd));
    }
}