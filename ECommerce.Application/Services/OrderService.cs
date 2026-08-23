using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
     IOrderRepository orderRepository,
     ICartRepository cartRepository,
     ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(
        int userId,
        CancellationToken cancellationToken = default)
        
    {
        _logger.LogInformation(
    "Creating order for UserId {UserId}",
    userId);
        // 1. Get user's cart
        var cart = await _cartRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (cart == null)
        {
            throw new InvalidOperationException(
                "Cart not found.");
        }

        // 2. Make sure cart contains items
        if (cart.CartItems == null ||
            !cart.CartItems.Any())
        {
            throw new InvalidOperationException(
                "Cannot create an order from an empty cart.");
        }

        // 3. Create the order
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = 0
        };

        // 4. Process every cart item
        foreach (var cartItem in cart.CartItems)
        {
            var product = cartItem.Product;

            if (product == null)
            {
                throw new InvalidOperationException(
                    $"Product {cartItem.ProductId} was not found.");
            }

            // 5. Check product availability
            if (!product.IsActive)
            {
                throw new InvalidOperationException(
                    $"Product '{product.Name}' is no longer available.");
            }

            // 6. Check stock
            if (product.Stock < cartItem.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.Name}'. " +
                    $"Available stock: {product.Stock}.");
            }

            // 7. Use CURRENT product price
            var unitPrice = product.Price;

            // 8. Calculate subtotal
            var subtotal = unitPrice * cartItem.Quantity;

            // 9. Create order item
            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity,
                Subtotal = subtotal
            };

            order.OrderItems.Add(orderItem);

            // 10. Calculate total
            order.TotalAmount += subtotal;

            // 11. Reduce stock
            product.Stock -= cartItem.Quantity;
        }

        // 12. Add order
        await _orderRepository.CreateAsync(
            order,
            cancellationToken);

        // 13. Remove the cart
        _cartRepository.RemoveCart(cart);

        // 14. Save everything together
        await _orderRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
    "Order {OrderId} created successfully for UserId {UserId} with total amount {TotalAmount}",
    order.Id,
    userId,
    order.TotalAmount);

        // 15. Convert to DTO
        return MapToDto(order);
    }

    public async Task<List<OrderSummaryDto>> GetUserOrdersAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetUserOrdersAsync(
            userId,
            cancellationToken);

        return orders
            .Select(order => new OrderSummaryDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ItemCount = order.OrderItems.Count
            })
            .ToList();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            userId,
            cancellationToken);

        if (order == null)
        {
            return null;
        }

        return MapToDto(order);
    }

    public async Task<bool> CancelOrderAsync(
    int orderId,
    int userId,
    CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            userId,
            cancellationToken);

        if (order == null)
        {
            return false;
        }

        if (order.Status != OrderStatus.Pending &&
            order.Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "This order cannot be cancelled.");
        }

        // Restore stock for every item in the order
        foreach (var orderItem in order.OrderItems)
        {
            var product =
                await _cartRepository.GetProductAsync(
                    orderItem.ProductId,
                    cancellationToken);

            if (product != null)
            {
                product.Stock += orderItem.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;

        await _orderRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
    "Order {OrderId} cancelled by UserId {UserId}",
    order.Id,
    userId);

        return true;
    }
    public async Task<bool> UpdateOrderStatusAsync(
    int orderId,
    OrderStatus newStatus,
    CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order == null)
        {
            return false;
        }

        var currentStatus = order.Status;

        if (currentStatus == newStatus)
        {
            return true;
        }

        var isValidTransition =
            currentStatus switch
            {
                OrderStatus.Pending =>
                    newStatus == OrderStatus.Confirmed ||
                    newStatus == OrderStatus.Cancelled,

                OrderStatus.Confirmed =>
                    newStatus == OrderStatus.Processing ||
                    newStatus == OrderStatus.Cancelled,

                OrderStatus.Processing =>
                    newStatus == OrderStatus.Shipped,

                OrderStatus.Shipped =>
                    newStatus == OrderStatus.Delivered,

                OrderStatus.Delivered =>
                    false,

                OrderStatus.Cancelled =>
                    false,

                _ => false
            };

        if (!isValidTransition)
        {
            throw new InvalidOperationException(
                $"Invalid order status transition: " +
                $"{currentStatus} → {newStatus}.");
        }

        order.Status = newStatus;

        await _orderRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
    public async Task<List<OrderSummaryDto>> GetAllOrdersAsync(
    CancellationToken cancellationToken = default)
    {
        var orders =
            await _orderRepository.GetAllOrdersAsync(
                cancellationToken);

        return orders
            .Select(order => new OrderSummaryDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ItemCount = order.OrderItems.Count
            })
            .ToList();
    }

    public async Task<OrderDto?> GetAdminOrderByIdAsync(
    int orderId,
    CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order == null)
        {
            return null;
        }

        return MapToDto(order);
    }
    public async Task<List<OrderSummaryDto>> GetAllOrdersAsync(
    OrderStatus? status = null,
    CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllOrdersAsync(
            status,
            cancellationToken);

        return orders
            .Select(order => new OrderSummaryDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ItemCount = order.OrderItems.Count
            })
            .ToList();
    }

    public async Task<List<OrderDto>> GetOrdersByStatusAsync(
    OrderStatus status,
    CancellationToken cancellationToken = default)
    {
        var orders =
            await _orderRepository.GetOrdersByStatusAsync(
                status,
                cancellationToken);

        return orders
            .Select(MapToDto)
            .ToList();
    }
    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,

            Payment = order.Payment == null
                ? null
                : new PaymentDto
                {
                    Id = order.Payment.Id,
                    OrderId = order.Payment.OrderId,
                    Amount = order.Payment.Amount,
                    Method = order.Payment.Method,
                    Status = order.Payment.Status,
                    TransactionId = order.Payment.TransactionId,
                    PaymentDate = order.Payment.PaymentDate
                },

            Items = order.OrderItems
                .Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    Subtotal = item.Subtotal
                })
                .ToList()
        };
    }

    
}