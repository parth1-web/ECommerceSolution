using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
    int orderId,
    CancellationToken cancellationToken = default);

    Task<List<Order>> GetUserOrdersAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Order> CreateAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
    Task<List<Order>> GetAllOrdersAsync(
    CancellationToken cancellationToken = default);
    Task<List<Order>> GetAllOrdersAsync(
    OrderStatus? status = null,
    CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrdersByStatusAsync(
    OrderStatus status,
    CancellationToken cancellationToken = default);
}