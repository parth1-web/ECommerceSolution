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
            .ReturnsAsync((Order order, CancellationToken _) => order);

        // Act

        var result =
            await _orderService.CreateOrderAsync(1);

        // Assert

        Assert.NotNull(result);

        Assert.Single(result.Items);

        Assert.Equal(1000, result.TotalAmount);

        Assert.Equal("Test Product", result.Items[0].ProductName);

        Assert.Equal(500, result.Items[0].UnitPrice);

        Assert.Equal(2, result.Items[0].Quantity);

        Assert.Equal(1000, result.Items[0].Subtotal);

        Assert.Equal(8, product.Stock);

        Assert.Equal(
            ECommerce.Domain.Enums.OrderStatus.Pending,
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
    }

}