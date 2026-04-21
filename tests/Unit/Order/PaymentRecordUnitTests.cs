using Domain.Base.Enum;
using Domain.Base.ValueObjects;
using Domain.Orders.Entities;

namespace Unit.Order;

public class PaymentRecordUnitTests
{
    // Create
    [Fact]
    public void Create_WithValidData_InstantiatesPaymentRecordAndSetsCreatedAt()
    {
        // Arrange
        var orderId = 1;
        var amount = 100.0m;
        var intentId = "pi_123";
        var clientSecret = "pi_123_secret_456";
        var currency = new CurrencyCode("USD");

        // Act
        var paymentRecord = PaymentRecord.Create(orderId, PaymentProvider.Mock0, intentId, PaymentStatus.Approved, amount, currency, clientSecret);

        // Assert
        Assert.NotNull(paymentRecord);
        Assert.True(paymentRecord.CreatedAt > DateTime.MinValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidOrderId_ThrowsArgumentOutOfRangeException(int orderId)
    {
        // Arrange
        var amount = 100.0m;
        var intentId = "pi_123";
        var clientSecret = "pi_123_secret_456";
        var currency = new CurrencyCode("USD");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            PaymentRecord.Create(orderId, PaymentProvider.Mock0, intentId, PaymentStatus.Approved, amount, currency, clientSecret));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        // Arrange
        var orderId = 1;
        var intentId = "pi_123";
        var clientSecret = "pi_123_secret_456";
        var currency = new CurrencyCode("USD");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            PaymentRecord.Create(orderId, PaymentProvider.Mock0, intentId, PaymentStatus.Approved, amount, currency, clientSecret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidIntentId_ThrowsArgumentNullException(string intentId)
    {
        // Arrange
        var orderId = 1;
        var amount = 100.0m;
        var clientSecret = "pi_123_secret_456";
        var currency = new CurrencyCode("USD");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            PaymentRecord.Create(orderId, PaymentProvider.Mock0, intentId, PaymentStatus.Approved, amount, currency, clientSecret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidClientSecret_ThrowsArgumentNullException(string clientSecret)
    {
        // Arrange
        var orderId = 1;
        var amount = 100.0m;
        var intentId = "pi_123";
        var currency = new CurrencyCode("USD");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            PaymentRecord.Create(orderId, PaymentProvider.Mock0, intentId, PaymentStatus.Approved, amount, currency, clientSecret));
    }
    
    // ChangePaymentStatus
    [Fact]
    public void ChangePaymentStatus_WithValidNewStatus_UpdatesStatusAndSetsUpdatedAt()
    {
        // Arrange
        var currency = new CurrencyCode("USD");
        var paymentRecord = PaymentRecord.Create(1, PaymentProvider.Mock0, "pi_123", PaymentStatus.Created, 100.0m, currency, "pi_123_secret_456");
        var newStatus = PaymentStatus.Approved;

        // Act
        paymentRecord.ChangePaymentStatus(newStatus);

        // Assert
        Assert.Equal(newStatus, paymentRecord.Status);
        Assert.NotNull(paymentRecord.UpdatedAt);
    }

    [Fact]
    public void ChangePaymentStatus_WhenCurrentStatusIsApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var currency = new CurrencyCode("USD");
        var paymentRecord = PaymentRecord.Create(1, PaymentProvider.Mock0, "pi_123", PaymentStatus.Approved, 100.0m, currency, "pi_123_secret_456");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => paymentRecord.ChangePaymentStatus(PaymentStatus.Failed));
    }

    [Fact]
    public void ChangePaymentStatus_WhenCurrentStatusIsFailed_ThrowsInvalidOperationException()
    {
        // Arrange
        var currency = new CurrencyCode("USD");
        var paymentRecord = PaymentRecord.Create(1, PaymentProvider.Mock0, "pi_123", PaymentStatus.Failed, 100.0m, currency, "pi_123_secret_456");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => paymentRecord.ChangePaymentStatus(PaymentStatus.Approved));
    }
}