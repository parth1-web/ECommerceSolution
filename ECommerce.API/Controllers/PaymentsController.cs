
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
    // POST: api/Payments
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
    // GET: api/Payments/order/{orderId}
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
    // KHALTI INITIATE
    // POST: api/Payments/khalti/initiate
    // =========================================================

    [Authorize]
    [HttpPost("khalti/initiate")]
    public async Task<IActionResult> InitiateKhaltiPayment(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var currentUserId))
        {
            return Unauthorized();
        }

        // -----------------------------------------------------
        // The payment method must be Khalti.
        // -----------------------------------------------------

        if (dto.Method != ECommerce.Domain.Enums.PaymentMethod.Khalti)
        {
            return BadRequest(new
            {
                message =
                    "Payment method must be Khalti."
            });
        }

        try
        {
            var result =
                await _paymentService
                    .InitiateKhaltiPaymentAsync(
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
    // KHALTI CALLBACK
    // GET: api/Payments/khalti/callback?pidx=...
    // =========================================================

    [AllowAnonymous]
    [HttpGet("khalti/callback")]
    public async Task<IActionResult> KhaltiCallback(
        [FromQuery] string? pidx,
        CancellationToken cancellationToken)
    {
        // -----------------------------------------------------
        // pidx is required.
        //
        // This also satisfies:
        //
        // KhaltiCallback_WithoutPidx_ReturnsBadRequest
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(pidx))
        {
            return BadRequest(new
            {
                success = false,
                message = "Khalti payment identifier is missing."
            });
        }

        try
        {
            var payment =
                await _paymentService
                    .VerifyKhaltiPaymentAsync(
                        pidx,
                        cancellationToken);

            return Ok(new
            {
                success = true,
                message =
                    "Khalti payment verified successfully.",
                payment
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }


    // =========================================================
    // ESEWA INITIATE
    // POST: api/Payments/esewa/initiate
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
                await _esewaPaymentService
                    .InitiatePaymentAsync(
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
    //
    // GET:
    // api/Payments/esewa/success?data=BASE64_DATA
    //
    // =========================================================

    [AllowAnonymous]
    [HttpGet("esewa/success")]
    public async Task<IActionResult> EsewaSuccess(
        [FromQuery] string? data,
        CancellationToken cancellationToken)
    {
        // =====================================================
        // STEP 1: Validate callback data
        // =====================================================

        if (string.IsNullOrWhiteSpace(data))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "eSewa callback data is missing."
            });
        }


        // =====================================================
        // STEP 2: Decode Base64
        // =====================================================

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
            return BadRequest(new
            {
                success = false,
                message =
                    "Invalid Base64 eSewa callback data."
            });
        }


        // =====================================================
        // STEP 3: Parse JSON
        // =====================================================

        JsonDocument document;

        try
        {
            document =
                JsonDocument.Parse(decodedData);
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Invalid eSewa callback JSON."
            });
        }


        using (document)
        {
            var root = document.RootElement;


            // =================================================
            // STEP 4: Extract transaction UUID
            // =================================================

            var transactionUuid =
                root.TryGetProperty(
                    "transaction_uuid",
                    out var transactionUuidElement)
                    ? transactionUuidElement.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(transactionUuid))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "eSewa transaction UUID is missing."
                });
            }


            // =================================================
            // STEP 5: Extract callback status
            // =================================================

            var status =
                root.TryGetProperty(
                    "status",
                    out var statusElement)
                    ? statusElement.GetString()
                    : null;


            // =================================================
            // STEP 6: Callback status check
            //
            // IMPORTANT:
            //
            // COMPLETE is NOT trusted as proof of payment.
            //
            // We still perform server-side verification below.
            // =================================================

            if (!string.Equals(
                    status,
                    "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    $"eSewa payment status: " +
                    $"{status ?? "UNKNOWN"}");
            }


            // =================================================
            // STEP 7: Find payment using transaction UUID
            //
            // PaymentService internally uses:
            //
            // GetByTransactionIdAsync(...)
            // =================================================

            var payment =
                await _paymentService
                    .GetPaymentByTransactionUuidAsync(
                        transactionUuid,
                        cancellationToken);

            if (payment == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Payment not found for this transaction."
                });
            }


            // =================================================
            // STEP 8: Get OrderId
            // =================================================

            var orderId =
                payment.OrderId;


            // =================================================
            // STEP 9: SERVER-SIDE VERIFICATION
            //
            // EsewaPaymentService will call eSewa's status API
            // and verify:
            //
            // - payment exists
            // - payment method is eSewa
            // - status is COMPLETE
            // - transaction UUID matches
            // - amount matches
            //
            // Then it marks:
            //
            // Payment = Paid
            // Order = Confirmed
            // =================================================

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
            catch (InvalidOperationException ex)
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    ex.Message);
            }


            // =================================================
            // STEP 10: Verification failed
            // =================================================

            if (!verified)
            {
                return RedirectToMvcFailure(
                    transactionUuid,
                    "eSewa payment verification failed.");
            }


            // =================================================
            // STEP 11: Verified successfully
            // =================================================

            return RedirectToMvcSuccess(
                orderId,
                transactionUuid);
        }
    }


    // =========================================================
    // ESEWA FAILURE CALLBACK
    //
    // GET:
    // api/Payments/esewa/failure
    //
    // =========================================================

    [AllowAnonymous]
    [HttpGet("esewa/failure")]
    public IActionResult EsewaFailure(
        [FromQuery] string? data)
    {
        // -----------------------------------------------------
        // Existing integration test expects:
        //
        // GET /api/Payments/esewa/failure
        // without data
        //
        // => 400 BadRequest
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(data))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "eSewa payment failed: failure callback data is missing."
            });
        }


        string? transactionUuid = null;

        try
        {
            // -------------------------------------------------
            // Decode Base64
            // -------------------------------------------------

            var decodedBytes =
                Convert.FromBase64String(data);

            var decodedData =
                Encoding.UTF8.GetString(decodedBytes);


            // -------------------------------------------------
            // Parse JSON
            // -------------------------------------------------

            using var document =
                JsonDocument.Parse(decodedData);

            var root =
                document.RootElement;


            // -------------------------------------------------
            // Extract transaction UUID
            // -------------------------------------------------

            if (root.TryGetProperty(
                    "transaction_uuid",
                    out var transactionUuidElement))
            {
                transactionUuid =
                    transactionUuidElement.GetString();
            }
        }
        catch (FormatException)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Invalid Base64 eSewa failure data."
            });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Invalid eSewa failure callback JSON."
            });
        }


        // -----------------------------------------------------
        // Redirect customer to MVC failure page.
        // -----------------------------------------------------

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
        // Replace this with your actual MVC application's URL.
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
        // Replace this with your actual MVC application's URL.
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

