namespace ECommerce.Application.DTOs.Payments;

public class ESewaPaymentRequestDto
{
    public decimal Amount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ProductServiceCharge { get; set; }

    public decimal ProductDeliveryCharge { get; set; }

    public decimal TotalAmount { get; set; }

    public string TransactionUuid { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = string.Empty;

    public string FailureUrl { get; set; } = string.Empty;
}