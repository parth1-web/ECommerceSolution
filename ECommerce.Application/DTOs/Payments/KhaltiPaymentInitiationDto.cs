
namespace ECommerce.Application.DTOs.Payments;

public class KhaltiPaymentInitiationDto
{
    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Pidx { get; set; } = string.Empty;

    public string PaymentUrl { get; set; } = string.Empty;
}

