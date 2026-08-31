
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetByTransactionIdAsync(
        string transactionId,
        CancellationToken cancellationToken = default);

    Task<Payment> CreateAsync(
        Payment payment,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
