
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IEsewaPaymentService _esewaPaymentService;

    public PaymentsController(
        IPaymentService paymentService,
        IEsewaPaymentService esewaPaymentService)
    {
        _paymentService = paymentService;
        _esewaPaymentService = esewaPaymentService;
    }


    // =========================================================
    // CREATE PAYMENT
    // =========================================================

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _paymentService.CreatePaymentAsync(
                    dto,
                    currentUserId,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // GET PAYMENT BY ORDER ID
    // =========================================================

    [Authorize]
    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetPaymentByOrderId(
        int orderId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var currentUserId))
        {
            return Unauthorized();
        }

        var payment =
            await _paymentService.GetPaymentByOrderIdAsync(
                orderId,
                currentUserId,
                cancellationToken);

        if (payment == null)
        {
            return NotFound(new
            {
                message = "Payment not found."
            });
        }

        return Ok(payment);
    }


    // =========================================================
    // INITIATE ESEWA PAYMENT
    // =========================================================
    //
    // POST:
    // /api/Payments/esewa/initiate
    //
    // MVC calls this endpoint.
    //
    // The API:
    // 1. Finds the order
    // 2. Creates Pending payment
    // 3. Generates transaction_uuid
    // 4. Generates eSewa signature
    // 5. Returns form data to MVC
    //
    // =========================================================

    [Authorize]
    [HttpPost("esewa/initiate")]
    public async Task<IActionResult> InitiateEsewaPayment(
        [FromBody] CreateESewaPaymentDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _esewaPaymentService.InitiatePaymentAsync(
                    dto.OrderId,
                    currentUserId,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // ESEWA SUCCESS CALLBACK
    // =========================================================
    //
    // eSewa redirects here:
    //
    // GET
    // /api/Payments/esewa/success?data=BASE64_DATA
    //
    // Example decoded data:
    //
    // {
    //   "transaction_code": "000GXJM",
    //   "status": "COMPLETE",
    //   "total_amount": "800.0",
    //   "transaction_uuid": "...",
    //   "product_code": "EPAYTEST",
    //   "signed_field_names": "...",
    //   "signature": "..."
    // }
    //
    // =========================================================

    [AllowAnonymous]
    [HttpGet("esewa/success")]
    public async Task<IActionResult> EsewaSuccess(
        [FromQuery] string data,
        CancellationToken cancellationToken)
    {
        // -----------------------------------------------------
        // STEP 1: Validate data
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(data))
        {
            return RedirectToMvcFailure(
                null,
                "eSewa callback data is missing.");
        }


        // -----------------------------------------------------
        // STEP 2: Base64 decode
        // -----------------------------------------------------

        string decodedData;

        try
        {
            var decodedBytes =
                Convert.FromBase64String(data);

            decodedData =
                Encoding.UTF8.GetString(decodedBytes);
        }
        catch (FormatException)
        {
            return RedirectToMvcFailure(
                null,
                "Invalid eSewa callback data.");
        }


        // -----------------------------------------------------
        // STEP 3: Parse JSON
        // -----------------------------------------------------

        JsonDocument document;

        try
        {
            document =
                JsonDocument.Parse(decodedData);
        }
        catch (JsonException)
        {
            return RedirectToMvcFailure(
                null,
                "Invalid eSewa callback JSON.");
        }


        using (document)
        {
            var root = document.RootElement;


            // -------------------------------------------------
            // STEP 4: Extract callback status
            // -------------------------------------------------

            var status =
                root.TryGetProperty(
                    "status",
                    out var statusElement)
                    ? statusElement.GetString()
                    : null;


            // -------------------------------------------------
            // STEP 5: Extract transaction_uuid
            // -------------------------------------------------

            var transactionUuid =
                root.TryGetProperty(
                    "transaction_uuid",
                    out var transactionUuidElement)
                    ? transactionUuidElement.GetString()
                    : null;


            // -------------------------------------------------
            // Validate transaction UUID
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(transactionUuid))
            {
                return RedirectToMvcFailure(
                    null,
                    "eSewa transaction UUID is missing.");
            }


            // -------------------------------------------------
            // STEP 6: Check callback status
            // -------------------------------------------------
            //
            // IMPORTANT:
            //
            // We do NOT mark the payment as Paid here.
            //
            // The callback status is only an indication that
            // we should continue to server-side verification.
            //
            // -------------------------------------------------

            if (!string.Equals(
                    status,
                    "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    $"eSewa payment status: {status ?? "UNKNOWN"}");
            }


            // -------------------------------------------------
            // STEP 7: Find payment using transaction UUID
            // -------------------------------------------------
            //
            // PaymentService internally calls:
            //
            // IPaymentRepository.GetByTransactionIdAsync(...)
            //
            // -------------------------------------------------

            var payment =
                await _paymentService
                    .GetPaymentByTransactionUuidAsync(
                        transactionUuid,
                        cancellationToken);

            if (payment == null)
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    "Payment not found for this transaction.");
            }


            // -------------------------------------------------
            // STEP 8: Get OrderId from Payment
            // -------------------------------------------------

            var orderId = payment.OrderId;


            // -------------------------------------------------
            // STEP 9: SERVER-SIDE VERIFICATION
            // -------------------------------------------------
            //
            // This is the important security step.
            //
            // EsewaPaymentService.VerifyPaymentAsync()
            // calls the eSewa transaction status API.
            //
            // It verifies:
            //
            // 1. Payment exists
            // 2. Payment method is eSewa
            // 3. eSewa status is COMPLETE
            // 4. transaction_uuid matches
            // 5. amount matches
            //
            // Then it:
            //
            // 6. Marks Payment as Paid
            // 7. Changes Pending Order to Confirmed
            //
            // -------------------------------------------------

            bool verified;

            try
            {
                verified =
                    await _esewaPaymentService
                        .VerifyPaymentAsync(
                            orderId,
                            transactionUuid,
                            cancellationToken);
            }
            catch (Exception ex)
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    $"Payment verification failed: {ex.Message}");
            }


            // -------------------------------------------------
            // STEP 10: Verification failed
            // -------------------------------------------------

            if (!verified)
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    "eSewa payment verification failed.");
            }


            // -------------------------------------------------
            // STEP 11: Payment successfully verified
            // -------------------------------------------------
            //
            // At this point:
            //
            // Payment.Status = Paid
            //
            // Order.Status = Confirmed
            //
            // The browser is redirected back to MVC.
            //
            // -------------------------------------------------

            return RedirectToMvcSuccess(
                orderId,
                transactionUuid);
        }
    }


    // =========================================================
    // ESEWA FAILURE CALLBACK
    // =========================================================

    [AllowAnonymous]
    [HttpGet("esewa/failure")]
    public IActionResult EsewaFailure(
        [FromQuery] string? data)
    {
        string? transactionUuid = null;

        // -----------------------------------------------------
        // eSewa may send callback data here as well.
        // Try to decode it if available.
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(data))
        {
            try
            {
                var decodedBytes =
                    Convert.FromBase64String(data);

                var decodedData =
                    Encoding.UTF8.GetString(decodedBytes);

                using var document =
                    JsonDocument.Parse(decodedData);

                var root = document.RootElement;

                if (root.TryGetProperty(
                        "transaction_uuid",
                        out var transactionUuidElement))
                {
                    transactionUuid =
                        transactionUuidElement.GetString();
                }
            }
            catch
            {
                // Failure callback should still redirect to MVC
                // even if the callback data cannot be decoded.
            }
        }


        return RedirectToMvcFailure(
            transactionUuid,
            "eSewa payment was not completed.");
    }


    // =========================================================
    // MVC SUCCESS REDIRECT
    // =========================================================

    private IActionResult RedirectToMvcSuccess(
        int orderId,
        string transactionUuid)
    {
        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Replace this with the actual URL of your MVC app.
        //
        // Example:
        //
        // https://localhost:xxxx/Payments/Success
        //
        // -----------------------------------------------------

        var mvcUrl =
            "https://localhost:7000/Payments/Success";

        var redirectUrl =
            $"{mvcUrl}" +
            $"?orderId={Uri.EscapeDataString(
                orderId.ToString())}" +
            $"&transactionUuid={Uri.EscapeDataString(
                transactionUuid)}";

        return Redirect(redirectUrl);
    }


    // =========================================================
    // MVC FAILURE REDIRECT
    // =========================================================

    private IActionResult RedirectToMvcFailure(
        string? transactionUuid,
        string message)
    {
        // -----------------------------------------------------
        // Replace with actual MVC failure page URL.
        // -----------------------------------------------------

        var mvcUrl =
            "https://localhost:7000/Payments/Failure";

        var redirectUrl =
            $"{mvcUrl}" +
            $"?message={Uri.EscapeDataString(message)}";

        if (!string.IsNullOrWhiteSpace(transactionUuid))
        {
            redirectUrl +=
                $"&transactionUuid=" +
                Uri.EscapeDataString(transactionUuid);
        }

        return Redirect(redirectUrl);
    }
}

