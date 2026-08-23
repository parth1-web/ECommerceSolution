
using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

public interface IKhaltiPaymentService
{
    Task<KhaltiInitiateResult> InitiatePaymentAsync(
        int orderId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<KhaltiLookupResult> VerifyPaymentAsync(
        string pidx,
        CancellationToken cancellationToken = default);
}

