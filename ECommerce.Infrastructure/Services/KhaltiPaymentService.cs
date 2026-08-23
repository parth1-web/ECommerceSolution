
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Configuration;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public class KhaltiPaymentService : IKhaltiPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly KhaltiSettings _settings;

    public KhaltiPaymentService(
        HttpClient httpClient,
        IOptions<KhaltiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<KhaltiInitiateResult> InitiatePaymentAsync(
        int orderId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var amountInPaisa = checked(
            (long)Math.Round(
                amount * 100m,
                MidpointRounding.AwayFromZero));

        var request = new KhaltiInitiateRequestDto
        {
            ReturnUrl = _settings.ReturnUrl,
            WebsiteUrl = _settings.WebsiteUrl,
            Amount = amountInPaisa,
            PurchaseOrderId = orderId.ToString(),
            PurchaseOrderName = $"Order #{orderId}"
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.BaseUrl.TrimEnd('/')}/epayment/initiate/");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Key",
                _settings.SecretKey);

        httpRequest.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Khalti payment initiation failed. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        var result =
            JsonSerializer.Deserialize<KhaltiInitiateResponseDto>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result == null || string.IsNullOrWhiteSpace(result.Pidx))
        {
            throw new InvalidOperationException(
                "Khalti returned an invalid payment initiation response.");
        }

        return new KhaltiInitiateResult
        {
            Pidx = result.Pidx,
            PaymentUrl = result.PaymentUrl
        };
    }

    public async Task<KhaltiLookupResult> VerifyPaymentAsync(
        string pidx,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            pidx
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.BaseUrl.TrimEnd('/')}/epayment/lookup/");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Key",
                _settings.SecretKey);

        httpRequest.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Khalti payment verification failed. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        var result =
            JsonSerializer.Deserialize<KhaltiLookupResult>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result == null)
        {
            throw new InvalidOperationException(
                "Khalti returned an invalid verification response.");
        }

        return result;
    }
}

