
namespace ECommerce.Application.DTOs.Payments;

public class KhaltiLookupResult
{
    public string Pidx { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public long TotalAmount { get; set; }

    public string TransactionId { get; set; } = string.Empty;

    public string PurchaseOrderId { get; set; } = string.Empty;
}
