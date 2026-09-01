using ECommerce.Application.Configuration;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ECommerce.Infrastructure.Services;

public class EsewaPaymentService : IEsewaPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ESewaSettings _settings;
    private readonly AppDbContext _context;
    private readonly ILogger<EsewaPaymentService> _logger;
    

    public EsewaPaymentService(
    HttpClient httpClient,
    IOptions<ESewaSettings> options,
    AppDbContext context,
    ILogger<EsewaPaymentService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _context = context;
        _logger = logger;
    }

    public async Task<ESewaPaymentInitiationDto> InitiatePaymentAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(
                o => o.Id == orderId &&
                     o.UserId == userId,
                cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException(
                "Order not found.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cannot make payment for a cancelled order.");
        }

        if (order.Payment != null)
        {
            throw new InvalidOperationException(
                "Payment already exists for this order.");
        }

        if (order.TotalAmount <= 0)
        {
            throw new InvalidOperationException(
                "Order amount must be greater than zero.");
        }

        var transactionUuid = Guid.NewGuid().ToString();

        var amount = order.TotalAmount;

        var amountText = amount.ToString(
            "0.00",
            CultureInfo.InvariantCulture);

        var signatureMessage =
            $"total_amount={amountText}," +
            $"transaction_uuid={transactionUuid}," +
            $"product_code={_settings.ProductCode}";

        var signature = GenerateSignature(
            signatureMessage,
            _settings.SecretKey);

        var payment = new ECommerce.Domain.Entities.Payment
        {
            OrderId = order.Id,
            Amount = amount,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Pending,
            TransactionId = transactionUuid,
            PaymentDate = DateTime.UtcNow
        };

        await _context.Payments.AddAsync(
            payment,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
    "eSewa payment initiated for OrderId {OrderId}. TransactionUuid: {TransactionUuid}, Amount: {Amount}",
    order.Id,
    transactionUuid,
    amount);

        var formData = new Dictionary<string, string>
        {
            ["amount"] = amountText,

            ["tax_amount"] = "0",

            ["total_amount"] = amountText,

            ["transaction_uuid"] = transactionUuid,

            ["product_code"] = _settings.ProductCode,

            ["product_service_charge"] = "0",

            ["product_delivery_charge"] = "0",

            ["success_url"] = _settings.SuccessUrl,

            ["failure_url"] = _settings.FailureUrl,

            ["signed_field_names"] =
                "total_amount,transaction_uuid,product_code",

            ["signature"] = signature
        };

        return new ESewaPaymentInitiationDto
        {
            OrderId = order.Id,

            Amount = amount,

            TransactionUuid = transactionUuid,

            PaymentUrl = _settings.BaseUrl,

            FormData = formData
        };
    }

    public async Task<bool> VerifyPaymentAsync(
        int orderId,
        string transactionUuid,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(
                p => p.OrderId == orderId &&
                     p.TransactionId == transactionUuid,
                cancellationToken);

        if (payment == null)
        {
            return false;
        }

        if (payment.Method != PaymentMethod.ESewa)
        {
            return false;
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            return true;
        }

        var query =
            $"product_code={Uri.EscapeDataString(_settings.ProductCode)}" +
            $"&total_amount={Uri.EscapeDataString(payment.Amount.ToString("0.00", CultureInfo.InvariantCulture))}" +
            $"&transaction_uuid={Uri.EscapeDataString(transactionUuid)}";

        var requestUrl = $"{_settings.StatusUrl}?{query}";

        using var response = await _httpClient.GetAsync(
            requestUrl,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        using var document = JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        var status =
            root.TryGetProperty(
                "status",
                out var statusElement)
                ? statusElement.GetString()
                : null;

        var responseTransactionUuid =
            root.TryGetProperty(
                "transaction_uuid",
                out var uuidElement)
                ? uuidElement.GetString()
                : null;

        var responseTotalAmount =
            root.TryGetProperty(
                "total_amount",
                out var amountElement)
                ? amountElement.GetString()
                : null;

        if (!string.Equals(
                status,
                "COMPLETE",
                StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        if (!string.Equals(
                responseTransactionUuid,
                transactionUuid,
                StringComparison.Ordinal))
        {
            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        if (!decimal.TryParse(
                responseTotalAmount,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var verifiedAmount))
        {
            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        if (verifiedAmount != payment.Amount)
        {
            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        var transactionCode =
            root.TryGetProperty(
                "transaction_code",
                out var transactionCodeElement)
                ? transactionCodeElement.GetString()
                : null;

        payment.Status = PaymentStatus.Paid;
        _logger.LogInformation(
    "eSewa payment completed successfully for OrderId {OrderId}. TransactionUuid: {TransactionUuid}",
    payment.OrderId,
    transactionUuid);

        if (!string.IsNullOrWhiteSpace(transactionCode))
        {
            payment.ESewaTransactionCode = transactionCode;
        }

        if (payment.Order.Status == OrderStatus.Pending)
        {
            payment.Order.Status = OrderStatus.Confirmed;
        }

        await _context.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private static string GenerateSignature(
        string message,
        string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);

        var hash = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hash);
    }
}