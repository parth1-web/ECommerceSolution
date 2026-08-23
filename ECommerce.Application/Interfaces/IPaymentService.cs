
using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(
        CreatePaymentDto dto,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PaymentDto?> GetPaymentByOrderIdAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<KhaltiPaymentInitiationDto> InitiateKhaltiPaymentAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> VerifyKhaltiPaymentAsync(
        string pidx,
        CancellationToken cancellationToken = default);
}

