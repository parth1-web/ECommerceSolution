namespace ECommerce.Application.DTOs.Payments;

public class ESewaPaymentInitiationDto
{
    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionUuid { get; set; } = string.Empty;

    public string PaymentUrl { get; set; } = string.Empty;

    public Dictionary<string, string> FormData { get; set; }
        = new();
}