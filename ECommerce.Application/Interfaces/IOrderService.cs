using ECommerce.Application.DTOs.Orders;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<OrderSummaryDto>> GetUserOrdersAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<OrderDto?> GetOrderByIdAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> CancelOrderAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateOrderStatusAsync(
        int orderId,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default);

    Task<List<OrderSummaryDto>> GetAllOrdersAsync(
  CancellationToken cancellationToken = default);

    Task<OrderDto?> GetAdminOrderByIdAsync(
    int orderId,
    CancellationToken cancellationToken = default);

    Task<List<OrderSummaryDto>> GetAllOrdersAsync(
    OrderStatus? status = null,
    CancellationToken cancellationToken = default);

    Task<List<OrderDto>> GetOrdersByStatusAsync(
    OrderStatus status,
    CancellationToken cancellationToken = default);
}