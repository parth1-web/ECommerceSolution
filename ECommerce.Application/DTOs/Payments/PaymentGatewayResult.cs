namespace ECommerce.Application.DTOs.Payments;

public class PaymentGatewayResult
{
    public bool Success { get; set; }

    public string? TransactionId { get; set; }

    public string? GatewayReference { get; set; }

    public string? Message { get; set; }
}