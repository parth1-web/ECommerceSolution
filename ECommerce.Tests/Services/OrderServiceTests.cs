using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace ECommerce.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;

    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock =
            new Mock<IOrderRepository>();

        _cartRepositoryMock =
            new Mock<ICartRepository>();

        _loggerMock =
            new Mock<ILogger<OrderService>>();

        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _cartRepositoryMock.Object,
            _loggerMock.Object);
    }


    // ============================================================
    // CREATE ORDER
    // ============================================================

    [Fact]
    public async Task CreateOrderAsync_WhenCartDoesNotExist_ThrowsException()
    {
        // Arrange

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(1));

        // Assert

        Assert.Equal(
            "Cart not found.",
            exception.Message);
    }


    [Fact]
    public async Task CreateOrderAsync_WhenCartIsEmpty_ThrowsException()
    {
        // Arrange

        var cart = new Cart
        {
            UserId = 1
        };

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(1));

        // Assert

        Assert.Equal(
            "Cannot create an order from an empty cart.",
            exception.Message);
    }


    [Fact]
    public async Task CreateOrderAsync_WhenProductIsInactive_ThrowsException()
    {
        // Arrange

        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 500,
            Stock = 10,
            IsActive = false
        };

        var cart = new Cart
        {
            UserId = 1,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    Product = product
                }
            }
        };

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(1));

        // Assert

        Assert.Equal(
            "Product 'Test Product' is no longer available.",
            exception.Message);
    }


    [Fact]
    public async Task CreateOrderAsync_WhenStockIsInsufficient_ThrowsException()
    {
        // Arrange

        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 500,
            Stock = 2,
            IsActive = true
        };

        var cart = new Cart
        {
            UserId = 1,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = product.Id,
                    Quantity = 5,
                    Product = product
                }
            }
        };

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(1));

        // Assert

        Assert.Equal(
            "Insufficient stock for 'Test Product'. Available stock: 2.",
            exception.Message);
    }


    [Fact]
    public async Task CreateOrderAsync_WhenCartIsValid_CreatesOrderSuccessfully()
    {
        // Arrange

        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 500,
            Stock = 10,
            IsActive = true
        };

        var cart = new Cart
        {
            UserId = 1,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    Product = product
                }
            }
        };

        _cartRepositoryMock
            .Setup(x => x.GetByUserIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (Order order, CancellationToken _) => order);

        // Act

        var result =
            await _orderService.CreateOrderAsync(1);

        // Assert

        Assert.NotNull(result);

        Assert.Single(result.Items);

        Assert.Equal(
            1000,
            result.TotalAmount);

        Assert.Equal(
            "Test Product",
            result.Items[0].ProductName);

        Assert.Equal(
            500,
            result.Items[0].UnitPrice);

        Assert.Equal(
            2,
            result.Items[0].Quantity);

        Assert.Equal(
            1000,
            result.Items[0].Subtotal);

        Assert.Equal(
            8,
            product.Stock);

        Assert.Equal(
            OrderStatus.Pending,
            result.Status);

        _cartRepositoryMock.Verify(
            x => x.RemoveCart(cart),
            Times.Once);

        _orderRepositoryMock.Verify(
            x => x.CreateAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    // ============================================================
    // CANCEL ORDER
    // ============================================================

    [Fact]
    public async Task CancelOrderAsync_WhenOrderDoesNotExist_ReturnsFalse()
    {
        // Arrange

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act

        var result =
            await _orderService.CancelOrderAsync(1, 1);

        // Assert

        Assert.False(result);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task CancelOrderAsync_WhenOrderIsPending_CancelsOrder()
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var result =
            await _orderService.CancelOrderAsync(1, 1);

        // Assert

        Assert.True(result);

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task CancelOrderAsync_WhenOrderIsConfirmed_CancelsOrder()
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Confirmed,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var result =
            await _orderService.CancelOrderAsync(1, 1);

        // Assert

        Assert.True(result);

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Theory]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CancelOrderAsync_WhenOrderCannotBeCancelled_ThrowsException(
        OrderStatus status)
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = status,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CancelOrderAsync(1, 1));

        // Assert

        Assert.Equal(
            "This order cannot be cancelled.",
            exception.Message);

        Assert.Equal(
            status,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task CancelOrderAsync_WhenOrderIsShipped_ThrowsException()
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Shipped,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CancelOrderAsync(1, 1));

        // Assert

        Assert.Equal(
            "This order cannot be cancelled.",
            exception.Message);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    // ============================================================
    // UPDATE ORDER STATUS
    // ============================================================

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenOrderDoesNotExist_ReturnsFalse()
    {
        // Arrange

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act

        var result =
            await _orderService.UpdateOrderStatusAsync(
                1,
                OrderStatus.Confirmed);

        // Assert

        Assert.False(result);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public async Task UpdateOrderStatusAsync_WhenTransitionIsValid_UpdatesStatusAndSaves(
        OrderStatus currentStatus,
        OrderStatus newStatus)
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = currentStatus,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var result =
            await _orderService.UpdateOrderStatusAsync(
                1,
                newStatus);

        // Assert

        Assert.True(result);

        Assert.Equal(
            newStatus,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    public async Task UpdateOrderStatusAsync_WhenCancellationTransitionIsValid_UpdatesStatusAndSaves(
        OrderStatus currentStatus,
        OrderStatus newStatus)
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = currentStatus,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var result =
            await _orderService.UpdateOrderStatusAsync(
                1,
                newStatus);

        // Assert

        Assert.True(result);

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]

    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]

    [InlineData(OrderStatus.Processing, OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled)]

    [InlineData(OrderStatus.Shipped, OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]

    [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Processing)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]

    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Processing)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Delivered)]
    public async Task UpdateOrderStatusAsync_WhenTransitionIsInvalid_ThrowsExceptionAndDoesNotSave(
        OrderStatus currentStatus,
        OrderStatus newStatus)
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = currentStatus,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.UpdateOrderStatusAsync(
                    1,
                    newStatus));

        // Assert

        Assert.Equal(
            $"Invalid order status transition: {currentStatus} → {newStatus}.",
            exception.Message);

        Assert.Equal(
            currentStatus,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Theory]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Processing)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]

    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Processing)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Delivered)]
    public async Task UpdateOrderStatusAsync_WhenOrderIsTerminal_CannotChangeStatus(
        OrderStatus terminalStatus,
        OrderStatus newStatus)
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = terminalStatus,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.UpdateOrderStatusAsync(
                    1,
                    newStatus));

        // Assert

        Assert.Equal(
            $"Invalid order status transition: {terminalStatus} → {newStatus}.",
            exception.Message);

        Assert.Equal(
            terminalStatus,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task UpdateOrderStatusAsync_WhenNewStatusIsSameAsCurrentStatus_ReturnsTrueWithoutSaving(
        OrderStatus status)
    {
        // Arrange

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = status,
            TotalAmount = 1000
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act

        var result =
            await _orderService.UpdateOrderStatusAsync(
                1,
                status);

        // Assert

        Assert.True(result);

        Assert.Equal(
            status,
            order.Status);

        _orderRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}