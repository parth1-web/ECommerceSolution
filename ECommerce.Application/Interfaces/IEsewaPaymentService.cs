using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

public interface IEsewaPaymentService
{
    Task<ESewaPaymentInitiationDto> InitiatePaymentAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyPaymentAsync(
        int orderId,
        string transactionUuid,
        CancellationToken cancellationToken = default);
}