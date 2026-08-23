
namespace ECommerce.Application.DTOs.Payments;

public class KhaltiInitiateRequestDto
{
    public string ReturnUrl { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public long Amount { get; set; }

    public string PurchaseOrderId { get; set; } = string.Empty;

    public string PurchaseOrderName { get; set; } = string.Empty;
}

