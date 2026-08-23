namespace ECommerce.Application.Configuration;

public class ESewaSettings
{
    public string ProductCode { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string StatusUrl { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = string.Empty;

    public string FailureUrl { get; set; } = string.Empty;
}