
namespace ECommerce.Application.DTOs.Payments;

public class KhaltiInitiateResponseDto
{
    public string Pidx { get; set; } = string.Empty;

    public string PaymentUrl { get; set; } = string.Empty;

    public string ExpiresAt { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }
}

