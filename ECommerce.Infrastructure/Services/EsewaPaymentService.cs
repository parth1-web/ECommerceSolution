
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

    // ============================================================
    // INITIATE ESEWA PAYMENT
    // ============================================================

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

        // --------------------------------------------------------
        // Generate stable transaction UUID.
        //
        // IMPORTANT:
        // TransactionId stores this UUID permanently.
        // It must NOT later be replaced with transaction_code.
        // --------------------------------------------------------

        var transactionUuid = Guid.NewGuid().ToString();

        var amount = order.TotalAmount;

        var amountText = amount.ToString(
            "0.00",
            CultureInfo.InvariantCulture);

        // --------------------------------------------------------
        // Generate eSewa signature
        // --------------------------------------------------------

        var signatureMessage =
            $"total_amount={amountText}," +
            $"transaction_uuid={transactionUuid}," +
            $"product_code={_settings.ProductCode}";

        var signature = GenerateSignature(
            signatureMessage,
            _settings.SecretKey);

        // --------------------------------------------------------
        // Create pending payment
        // --------------------------------------------------------

        var payment = new ECommerce.Domain.Entities.Payment
        {
            OrderId = order.Id,
            Amount = amount,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Pending,

            // Stable internal/provider correlation identifier
            TransactionId = transactionUuid,

            PaymentDate = DateTime.UtcNow
        };

        await _context.Payments.AddAsync(
            payment,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "eSewa payment initiated for OrderId {OrderId}. " +
            "TransactionUuid: {TransactionUuid}, Amount: {Amount}",
            order.Id,
            transactionUuid,
            amount);

        // --------------------------------------------------------
        // Build eSewa form data
        // --------------------------------------------------------

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


    // ============================================================
    // VERIFY ESEWA PAYMENT
    //
    // IMPORTANT:
    // Callback data alone is NOT trusted as proof of payment.
    //
    // This method calls eSewa's server-side status API and verifies:
    //
    // 1. Payment exists
    // 2. Payment method is eSewa
    // 3. Payment status is COMPLETE
    // 4. transaction_uuid matches
    // 5. total_amount matches
    //
    // Only then:
    //
    // Payment -> Paid
    // Order   -> Confirmed
    // ============================================================

    public async Task<bool> VerifyPaymentAsync(
        int orderId,
        string transactionUuid,
        CancellationToken cancellationToken = default)
    {
        // --------------------------------------------------------
        // Find payment using stable transaction UUID
        // --------------------------------------------------------

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

        // Already verified successfully
        if (payment.Status == PaymentStatus.Paid)
        {
            return true;
        }

        // --------------------------------------------------------
        // Build eSewa status verification URL
        // --------------------------------------------------------

        var query =
            $"product_code={Uri.EscapeDataString(_settings.ProductCode)}" +
            $"&total_amount={Uri.EscapeDataString(
                payment.Amount.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture))}" +
            $"&transaction_uuid={Uri.EscapeDataString(
                transactionUuid)}";

        var requestUrl =
            $"{_settings.StatusUrl}?{query}";

        // --------------------------------------------------------
        // Call eSewa server-side verification API
        // --------------------------------------------------------

        using var response = await _httpClient.GetAsync(
            requestUrl,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        // --------------------------------------------------------
        // HTTP request failed
        // --------------------------------------------------------

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "eSewa verification HTTP request failed for " +
                "OrderId {OrderId}, TransactionUuid {TransactionUuid}. " +
                "StatusCode: {StatusCode}. Response: {ResponseBody}",
                payment.OrderId,
                transactionUuid,
                response.StatusCode,
                responseBody);

            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        // --------------------------------------------------------
        // Parse verification response
        // --------------------------------------------------------

        using var document =
            JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        // --------------------------------------------------------
        // IMPORTANT JSON FIX
        //
        // eSewa may return some values as JSON Strings or Numbers.
        //
        // Using GetJsonValueAsString prevents:
        //
        // "The requested operation requires an element of type
        // 'String', but the target element has type 'Number'."
        // --------------------------------------------------------

        var status = GetJsonValueAsString(
            root,
            "status");

        var responseTransactionUuid = GetJsonValueAsString(
            root,
            "transaction_uuid");

        var responseTotalAmount = GetJsonValueAsString(
            root,
            "total_amount");

        // --------------------------------------------------------
        // Verify status
        // --------------------------------------------------------

        if (!string.Equals(
                status,
                "COMPLETE",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "eSewa verification returned non-complete status " +
                "for OrderId {OrderId}, TransactionUuid {TransactionUuid}. " +
                "Status: {Status}",
                payment.OrderId,
                transactionUuid,
                status);

            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        // --------------------------------------------------------
        // Verify transaction UUID
        // --------------------------------------------------------

        if (!string.Equals(
                responseTransactionUuid,
                transactionUuid,
                StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "eSewa transaction UUID mismatch for OrderId {OrderId}. " +
                "Expected: {ExpectedTransactionUuid}, Actual: {ActualTransactionUuid}",
                payment.OrderId,
                transactionUuid,
                responseTransactionUuid);

            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        // --------------------------------------------------------
        // Verify amount
        // --------------------------------------------------------

        if (!decimal.TryParse(
                responseTotalAmount,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var verifiedAmount))
        {
            _logger.LogWarning(
                "Unable to parse eSewa verified amount for " +
                "OrderId {OrderId}, TransactionUuid {TransactionUuid}. " +
                "Value: {ResponseTotalAmount}",
                payment.OrderId,
                transactionUuid,
                responseTotalAmount);

            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        if (verifiedAmount != payment.Amount)
        {
            _logger.LogWarning(
                "eSewa amount mismatch for OrderId {OrderId}. " +
                "Expected: {ExpectedAmount}, Actual: {ActualAmount}",
                payment.OrderId,
                payment.Amount,
                verifiedAmount);

            payment.Status = PaymentStatus.Failed;

            await _context.SaveChangesAsync(
                cancellationToken);

            return false;
        }

        // --------------------------------------------------------
        // Extract provider transaction code
        //
        // IMPORTANT:
        // This must NOT overwrite TransactionId.
        // --------------------------------------------------------

        var transactionCode = GetJsonValueAsString(
            root,
            "transaction_code");

        // --------------------------------------------------------
        // Payment successfully verified
        // --------------------------------------------------------

        payment.Status = PaymentStatus.Paid;

        if (!string.IsNullOrWhiteSpace(transactionCode))
        {
            payment.ESewaTransactionCode =
                transactionCode;
        }

        // --------------------------------------------------------
        // Confirm order
        // --------------------------------------------------------

        if (payment.Order.Status == OrderStatus.Pending)
        {
            payment.Order.Status =
                OrderStatus.Confirmed;
        }

        await _context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "eSewa payment completed successfully for " +
            "OrderId {OrderId}. TransactionUuid: {TransactionUuid}",
            payment.OrderId,
            transactionUuid);

        return true;
    }


    // ============================================================
    // SAFE JSON VALUE EXTRACTION
    //
    // Handles both:
    //
    // "total_amount": "100.00"
    //
    // and:
    //
    // "total_amount": 100
    //
    // without throwing InvalidOperationException.
    // ============================================================

    private static string? GetJsonValueAsString(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(
                propertyName,
                out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String =>
                element.GetString(),

            JsonValueKind.Number =>
                element.GetRawText(),

            JsonValueKind.True =>
                "true",

            JsonValueKind.False =>
                "false",

            _ => null
        };
    }


    // ============================================================
    // GENERATE ESEWA HMAC SHA256 SIGNATURE
    // ============================================================

    private static string GenerateSignature(
        string message,
        string secretKey)
    {
        var keyBytes =
            Encoding.UTF8.GetBytes(secretKey);

        var messageBytes =
            Encoding.UTF8.GetBytes(message);

        using var hmac =
            new HMACSHA256(keyBytes);

        var hash =
            hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hash);
    }
}

