using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Payments;

public class CreatePaymentDto
{
    public int OrderId { get; set; }

    public PaymentMethod Method { get; set; }
}