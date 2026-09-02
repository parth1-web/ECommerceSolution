using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ECommerce.Application.Configuration;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    //
    // FLOW:
    //
    // No Payment
    //      -> Create Pending Payment
    //
    // Pending Payment
    //      -> Reuse Existing Transaction UUID
    //
    // Failed Payment
    //      -> Generate New Transaction UUID
    //      -> Reset To Pending
    //
    // Paid Payment
    //      -> Reject
    // ============================================================

    public async Task<ESewaPaymentInitiationDto> InitiatePaymentAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // ========================================================
        // FIND ORDER
        // ========================================================

        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(
                o =>
                    o.Id == orderId &&
                    o.UserId == userId,
                cancellationToken);


        if (order == null)
        {
            throw new InvalidOperationException(
                "Order not found.");
        }


        // ========================================================
        // VALIDATE ORDER STATUS
        // ========================================================

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cannot make payment for a cancelled order.");
        }


        if (order.TotalAmount <= 0)
        {
            throw new InvalidOperationException(
                "Order amount must be greater than zero.");
        }


        // ========================================================
        // PAID PAYMENT
        //
        // Never allow another payment.
        // ========================================================

        if (order.Payment != null &&
            order.Payment.Status == PaymentStatus.Paid)
        {
            throw new InvalidOperationException(
                "This order has already been paid.");
        }


        Payment payment;

        string transactionUuid;


        // ========================================================
        // NO PAYMENT EXISTS
        //
        // Create a new Pending payment.
        // ========================================================

        if (order.Payment == null)
        {
            transactionUuid =
                Guid.NewGuid().ToString();

            payment = new Payment
            {
                OrderId = order.Id,

                Amount = order.TotalAmount,

                Method = PaymentMethod.ESewa,

                Status = PaymentStatus.Pending,

                // Stable provider correlation ID.
                TransactionId = transactionUuid,

                PaymentDate = DateTime.UtcNow
            };


            await _context.Payments.AddAsync(
                payment,
                cancellationToken);


            await _context.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "Created new eSewa payment. " +
                "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                order.Id,
                transactionUuid);
        }


        // ========================================================
        // EXISTING PENDING PAYMENT
        //
        // Reuse the same transaction UUID.
        //
        // This allows users to:
        //
        // - Close eSewa
        // - Return to the website
        // - Click eSewa again
        //
        // without creating duplicate payments.
        // ========================================================

        else if (order.Payment.Status == PaymentStatus.Pending)
        {
            payment = order.Payment;

            transactionUuid =
                payment.TransactionId;


            if (string.IsNullOrWhiteSpace(
                    transactionUuid))
            {
                transactionUuid =
                    Guid.NewGuid().ToString();

                payment.TransactionId =
                    transactionUuid;

                await _context.SaveChangesAsync(
                    cancellationToken);
            }


            _logger.LogInformation(
                "Reusing pending eSewa payment. " +
                "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                order.Id,
                transactionUuid);
        }


        // ========================================================
        // FAILED PAYMENT
        //
        // Generate a fresh transaction UUID.
        //
        // Reset:
        //
        // Payment -> Pending
        //
        // This allows a completely new payment attempt.
        // ========================================================

        else if (order.Payment.Status == PaymentStatus.Failed)
        {
            payment = order.Payment;

            transactionUuid =
                Guid.NewGuid().ToString();


            payment.TransactionId =
                transactionUuid;

            payment.ESewaTransactionCode =
                null;

            payment.Amount =
                order.TotalAmount;

            payment.Method =
                PaymentMethod.ESewa;

            payment.Status =
                PaymentStatus.Pending;

            payment.PaymentDate =
                DateTime.UtcNow;


            await _context.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "Reset failed eSewa payment for retry. " +
                "OrderId: {OrderId}, NewTransactionUuid: {TransactionUuid}",
                order.Id,
                transactionUuid);
        }


        // ========================================================
        // OTHER PAYMENT STATUS
        // ========================================================

        else
        {
            throw new InvalidOperationException(
                "The payment cannot be processed in its current state.");
        }


        // ========================================================
        // AMOUNT
        // ========================================================

        var amount =
            payment.Amount;


        var amountText =
            amount.ToString(
                "0.00",
                CultureInfo.InvariantCulture);


        // ========================================================
        // SIGNATURE MESSAGE
        //
        // The signed fields must exactly match:
        //
        // total_amount
        // transaction_uuid
        // product_code
        // ========================================================

        var signatureMessage =
            $"total_amount={amountText}," +
            $"transaction_uuid={transactionUuid}," +
            $"product_code={_settings.ProductCode}";


        var signature =
            GenerateSignature(
                signatureMessage,
                _settings.SecretKey);


        // ========================================================
        // BUILD ESEWA FORM DATA
        // ========================================================

        var formData =
            new Dictionary<string, string>
            {
                ["amount"] =
                    amountText,

                ["tax_amount"] =
                    "0",

                ["total_amount"] =
                    amountText,

                ["transaction_uuid"] =
                    transactionUuid,

                ["product_code"] =
                    _settings.ProductCode,

                ["product_service_charge"] =
                    "0",

                ["product_delivery_charge"] =
                    "0",

                ["success_url"] =
                    _settings.SuccessUrl,

                ["failure_url"] =
                    _settings.FailureUrl,

                ["signed_field_names"] =
                    "total_amount,transaction_uuid,product_code",

                ["signature"] =
                    signature
            };


        // ========================================================
        // RETURN PAYMENT INITIATION RESPONSE
        // ========================================================

        return new ESewaPaymentInitiationDto
        {
            OrderId =
                order.Id,

            Amount =
                amount,

            TransactionUuid =
                transactionUuid,

            PaymentUrl =
                _settings.BaseUrl,

            FormData =
                formData
        };
    }


    // ============================================================
    // VERIFY ESEWA PAYMENT
    //
    // IMPORTANT:
    //
    // Callback data is NOT trusted as proof of payment.
    //
    // We call eSewa's server-side Status API.
    // ============================================================

    public async Task<bool> VerifyPaymentAsync(
        int orderId,
        string transactionUuid,
        CancellationToken cancellationToken = default)
    {
        // ========================================================
        // FIND PAYMENT
        // ========================================================

        var payment =
            await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(
                    p =>
                        p.OrderId == orderId &&
                        p.TransactionId == transactionUuid,
                    cancellationToken);


        if (payment == null)
        {
            _logger.LogWarning(
                "eSewa payment not found. " +
                "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                orderId,
                transactionUuid);

            return false;
        }


        // ========================================================
        // VALIDATE PAYMENT METHOD
        // ========================================================

        if (payment.Method != PaymentMethod.ESewa)
        {
            _logger.LogWarning(
                "Payment method mismatch during eSewa verification. " +
                "OrderId: {OrderId}",
                orderId);

            return false;
        }


        // ========================================================
        // ALREADY PAID
        //
        // Makes verification idempotent.
        // ========================================================

        if (payment.Status == PaymentStatus.Paid)
        {
            _logger.LogInformation(
                "eSewa payment already verified. " +
                "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                orderId,
                transactionUuid);

            return true;
        }


        // ========================================================
        // BUILD ESEWA STATUS API URL
        // ========================================================

        var amountText =
            payment.Amount.ToString(
                "0.00",
                CultureInfo.InvariantCulture);


        var query =
            $"product_code={Uri.EscapeDataString(_settings.ProductCode)}" +
            $"&total_amount={Uri.EscapeDataString(amountText)}" +
            $"&transaction_uuid={Uri.EscapeDataString(transactionUuid)}";


        var requestUrl =
            $"{_settings.StatusUrl}?{query}";


        _logger.LogInformation(
            "Calling eSewa status API. " +
            "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
            payment.OrderId,
            transactionUuid);


        // ========================================================
        // CALL ESEWA STATUS API
        // ========================================================

        using var response =
            await _httpClient.GetAsync(
                requestUrl,
                cancellationToken);


        var responseBody =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);


        // ========================================================
        // HTTP FAILURE
        // ========================================================

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "eSewa verification HTTP request failed. " +
                "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}, " +
                "StatusCode: {StatusCode}, Response: {ResponseBody}",
                payment.OrderId,
                transactionUuid,
                response.StatusCode,
                responseBody);


            payment.Status =
                PaymentStatus.Failed;


            await _context.SaveChangesAsync(
                cancellationToken);


            return false;
        }


        // ========================================================
        // PARSE JSON RESPONSE
        // ========================================================

        JsonDocument document;


        try
        {
            document =
                JsonDocument.Parse(responseBody);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid JSON received from eSewa status API. " +
                "OrderId: {OrderId}",
                payment.OrderId);


            payment.Status =
                PaymentStatus.Failed;


            await _context.SaveChangesAsync(
                cancellationToken);


            return false;
        }


        using (document)
        {
            var root =
                document.RootElement;


            // ====================================================
            // EXTRACT VALUES SAFELY
            // ====================================================

            var status =
                GetJsonValueAsString(
                    root,
                    "status");


            var responseTransactionUuid =
                GetJsonValueAsString(
                    root,
                    "transaction_uuid");


            var responseTotalAmount =
                GetJsonValueAsString(
                    root,
                    "total_amount");


            // ====================================================
            // VERIFY STATUS
            // ====================================================

            if (!string.Equals(
                    status,
                    "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "eSewa returned non-complete status. " +
                    "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}, " +
                    "Status: {Status}",
                    payment.OrderId,
                    transactionUuid,
                    status);


                payment.Status =
                    PaymentStatus.Failed;


                await _context.SaveChangesAsync(
                    cancellationToken);


                return false;
            }


            // ====================================================
            // VERIFY TRANSACTION UUID
            // ====================================================

            if (!string.Equals(
                    responseTransactionUuid,
                    transactionUuid,
                    StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "eSewa transaction UUID mismatch. " +
                    "OrderId: {OrderId}, Expected: {Expected}, Actual: {Actual}",
                    payment.OrderId,
                    transactionUuid,
                    responseTransactionUuid);


                payment.Status =
                    PaymentStatus.Failed;


                await _context.SaveChangesAsync(
                    cancellationToken);


                return false;
            }


            // ====================================================
            // PARSE VERIFIED AMOUNT
            // ====================================================

            if (!decimal.TryParse(
                    responseTotalAmount,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var verifiedAmount))
            {
                _logger.LogWarning(
                    "Unable to parse verified eSewa amount. " +
                    "OrderId: {OrderId}, Value: {Value}",
                    payment.OrderId,
                    responseTotalAmount);


                payment.Status =
                    PaymentStatus.Failed;


                await _context.SaveChangesAsync(
                    cancellationToken);


                return false;
            }


            // ====================================================
            // VERIFY AMOUNT
            // ====================================================

            if (verifiedAmount != payment.Amount)
            {
                _logger.LogWarning(
                    "eSewa amount mismatch. " +
                    "OrderId: {OrderId}, Expected: {Expected}, Actual: {Actual}",
                    payment.OrderId,
                    payment.Amount,
                    verifiedAmount);


                payment.Status =
                    PaymentStatus.Failed;


                await _context.SaveChangesAsync(
                    cancellationToken);


                return false;
            }


            // ====================================================
            // GET PROVIDER TRANSACTION CODE
            //
            // IMPORTANT:
            //
            // Do NOT overwrite TransactionId.
            //
            // TransactionId remains our transaction_uuid.
            // ====================================================

            var transactionCode =
                GetJsonValueAsString(
                    root,
                    "transaction_code");


            // ====================================================
            // MARK PAYMENT AS PAID
            // ====================================================

            payment.Status =
                PaymentStatus.Paid;


            if (!string.IsNullOrWhiteSpace(
                    transactionCode))
            {
                payment.ESewaTransactionCode =
                    transactionCode;
            }


            // ====================================================
            // CONFIRM ORDER
            // ====================================================

            if (payment.Order.Status ==
                OrderStatus.Pending)
            {
                payment.Order.Status =
                    OrderStatus.Confirmed;
            }


            // ====================================================
            // SAVE CHANGES
            // ====================================================

            await _context.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "eSewa payment successfully verified. " +
                "OrderId: {OrderId}, TransactionUuid: {TransactionUuid}, " +
                "TransactionCode: {TransactionCode}",
                payment.OrderId,
                transactionUuid,
                transactionCode);


            return true;
        }
    }


    // ============================================================
    // SAFE JSON VALUE EXTRACTION
    //
    // Supports:
    //
    // "total_amount": "100.00"
    //
    // and:
    //
    // "total_amount": 100
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

            _ =>
                null
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
            Encoding.UTF8.GetBytes(
                secretKey);


        var messageBytes =
            Encoding.UTF8.GetBytes(
                message);


        using var hmac =
            new HMACSHA256(
                keyBytes);


        var hash =
            hmac.ComputeHash(
                messageBytes);


        return Convert.ToBase64String(
            hash);
    }
}