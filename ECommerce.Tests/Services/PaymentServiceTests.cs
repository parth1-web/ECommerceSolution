using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IKhaltiPaymentService> _khaltiPaymentServiceMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock =
            new Mock<IPaymentRepository>();

        _orderRepositoryMock =
            new Mock<IOrderRepository>();

        _khaltiPaymentServiceMock =
            new Mock<IKhaltiPaymentService>();

        _loggerMock =
            new Mock<ILogger<PaymentService>>();

        _paymentService = new PaymentService(
            _paymentRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _khaltiPaymentServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenOrderDoesNotExist_ThrowsException()
    {
        // Arrange

        var dto = new CreatePaymentDto
        {
            OrderId = 1,
            Method = PaymentMethod.CashOnDelivery
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.CreatePaymentAsync(
                    dto,
                    1));

        // Assert

        Assert.Equal(
            "Order not found.",
            exception.Message);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenPaymentAlreadyExists_ThrowsException()
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 1000
        };

        var existingPayment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.CashOnDelivery,
            Status = PaymentStatus.Pending
        };

        var dto = new CreatePaymentDto
        {
            OrderId = 1,
            Method = PaymentMethod.CashOnDelivery
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.CreatePaymentAsync(
                    dto,
                    1));

        // Assert

        Assert.Equal(
            "Payment already exists for this order.",
            exception.Message);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenOrderIsCancelled_ThrowsException()
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Cancelled,
            TotalAmount = 1000
        };

        var dto = new CreatePaymentDto
        {
            OrderId = 1,
            Method = PaymentMethod.CashOnDelivery
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.CreatePaymentAsync(
                    dto,
                    1));

        // Assert

        Assert.Equal(
            "Cannot make payment for a cancelled order.",
            exception.Message);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithCashOnDelivery_CreatesPaymentAndConfirmsOrder()
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 1500
        };

        var dto = new CreatePaymentDto
        {
            OrderId = 1,
            Method = PaymentMethod.CashOnDelivery
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) => payment);

        // Act

        var result =
            await _paymentService.CreatePaymentAsync(
                dto,
                1);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(1, result.OrderId);

        Assert.Equal(1500, result.Amount);

        Assert.Equal(
            PaymentMethod.CashOnDelivery,
            result.Method);

        Assert.Equal(
            PaymentStatus.Pending,
            result.Status);

        Assert.Equal(
            OrderStatus.Confirmed,
            order.Status);

        _paymentRepositoryMock.Verify(
            x => x.CreateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _paymentRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyKhaltiPaymentAsync_WhenPaymentDoesNotExist_ThrowsException()
    {
        // Arrange

        var khaltiResult = new KhaltiLookupResult
        {
            PurchaseOrderId = "1",
            TotalAmount = 100000,
            Status = "Completed",
            TransactionId = "TXN123"
        };

        _khaltiPaymentServiceMock
            .Setup(x => x.VerifyPaymentAsync(
                "PIDX123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(khaltiResult);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.VerifyKhaltiPaymentAsync(
                    "PIDX123"));

        // Assert

        Assert.Equal(
            "Payment not found for this order.",
            exception.Message);
    }

    [Fact]
    public async Task VerifyKhaltiPaymentAsync_WhenPaymentMethodIsNotKhalti_ThrowsException()
    {
        // Arrange

        var khaltiResult = new KhaltiLookupResult
        {
            PurchaseOrderId = "1",
            TotalAmount = 100000,
            Status = "Completed",
            TransactionId = "TXN123"
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.CashOnDelivery,
            Status = PaymentStatus.Pending
        };

        _khaltiPaymentServiceMock
            .Setup(x => x.VerifyPaymentAsync(
                "PIDX123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(khaltiResult);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.VerifyKhaltiPaymentAsync(
                    "PIDX123"));

        // Assert

        Assert.Equal(
            "This order is not using Khalti payment.",
            exception.Message);
    }

    [Fact]
    public async Task VerifyKhaltiPaymentAsync_WhenAmountDoesNotMatch_MarksPaymentFailed()
    {
        // Arrange

        var khaltiResult = new KhaltiLookupResult
        {
            PurchaseOrderId = "1",
            TotalAmount = 200000,
            Status = "Completed",
            TransactionId = "TXN123"
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.Khalti,
            Status = PaymentStatus.Pending
        };

        _khaltiPaymentServiceMock
            .Setup(x => x.VerifyPaymentAsync(
                "PIDX123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(khaltiResult);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.VerifyKhaltiPaymentAsync(
                    "PIDX123"));

        // Assert

        Assert.Equal(
            "Khalti payment amount does not match the order amount.",
            exception.Message);

        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);

        _paymentRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyKhaltiPaymentAsync_WhenPaymentIsNotCompleted_MarksPaymentFailed()
    {
        // Arrange

        var khaltiResult = new KhaltiLookupResult
        {
            PurchaseOrderId = "1",
            TotalAmount = 100000,
            Status = "Pending",
            TransactionId = "TXN123"
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.Khalti,
            Status = PaymentStatus.Pending
        };

        _khaltiPaymentServiceMock
            .Setup(x => x.VerifyPaymentAsync(
                "PIDX123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(khaltiResult);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _paymentService.VerifyKhaltiPaymentAsync(
                    "PIDX123"));

        // Assert

        Assert.Equal(
            "Khalti payment was not completed. Current status: Pending",
            exception.Message);

        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);
    }

    [Fact]
    public async Task VerifyKhaltiPaymentAsync_WhenPaymentIsCompleted_MarksPaymentPaid()
    {
        // Arrange

        var khaltiResult = new KhaltiLookupResult
        {
            PurchaseOrderId = "1",
            TotalAmount = 100000,
            Status = "Completed",
            TransactionId = "TXN123"
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.Khalti,
            Status = PaymentStatus.Pending
        };

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 1000
        };

        _khaltiPaymentServiceMock
            .Setup(x => x.VerifyPaymentAsync(
                "PIDX123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(khaltiResult);

        _paymentRepositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // IMPORTANT:
        // PaymentService uses the overload:
        // GetByIdAsync(orderId, cancellationToken)

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var result =
            await _paymentService.VerifyKhaltiPaymentAsync(
                "PIDX123");

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            PaymentStatus.Paid,
            result.Status);

        Assert.Equal(
            PaymentStatus.Paid,
            payment.Status);

        Assert.Equal(
            "TXN123",
            payment.TransactionId);

        Assert.Equal(
            OrderStatus.Confirmed,
            order.Status);

        _paymentRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }



}