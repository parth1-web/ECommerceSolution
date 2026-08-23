using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

public interface IPaymentGateway
{
    Task<KhaltiInitiateResult> InitiateAsync(
        int orderId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<KhaltiLookupResult> VerifyAsync(
        string reference,
        CancellationToken cancellationToken = default);
}