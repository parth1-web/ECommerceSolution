using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    // For eSewa, this stores the stable transaction_uuid used
    // for callback correlation and server-side verification.
    // For Khalti, this stores the provider transaction ID.
    public string? TransactionId { get; set; }

    // eSewa provider transaction_code returned after successful verification.
    public string? ESewaTransactionCode { get; set; }

    public DateTime PaymentDate { get; set; }

    public Order Order { get; set; } = null!;
}