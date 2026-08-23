using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public string? TransactionId { get; set; }

    public DateTime PaymentDate { get; set; }
}