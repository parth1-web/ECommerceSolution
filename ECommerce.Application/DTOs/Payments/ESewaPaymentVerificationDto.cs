namespace ECommerce.Application.DTOs.Payments;

public class ESewaPaymentVerificationDto
{
    public bool Success { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string TransactionUuid { get; set; } = string.Empty;

    public string? TransactionCode { get; set; }
}