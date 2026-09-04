using System.Security.Claims;
using System.Text;
using System.Text.Json;

using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IEsewaPaymentService _esewaPaymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IEsewaPaymentService esewaPaymentService,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _esewaPaymentService = esewaPaymentService;
        _configuration = configuration;
        _logger = logger;
    }


    // =========================================================
    // CREATE NORMAL PAYMENT
    //
    // POST: api/Payments
    // =========================================================

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _paymentService.CreatePaymentAsync(
                    dto,
                    currentUserId.Value,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Payment creation failed for UserId {UserId}.",
                currentUserId);

            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // GET PAYMENT BY ORDER ID
    //
    // GET: api/Payments/order/{orderId}
    // =========================================================

    [Authorize]
    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetPaymentByOrderId(
        int orderId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var payment =
            await _paymentService.GetPaymentByOrderIdAsync(
                orderId,
                currentUserId.Value,
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
    //
    // POST: api/Payments/khalti/initiate
    // =========================================================

    [Authorize]
    [HttpPost("khalti/initiate")]
    public async Task<IActionResult> InitiateKhaltiPayment(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
        {
            return Unauthorized();
        }


        // =====================================================
        // VALIDATE PAYMENT METHOD
        // =====================================================

        if (dto.Method != PaymentMethod.Khalti)
        {
            return BadRequest(new
            {
                message = "Payment method must be Khalti."
            });
        }

        try
        {
            var result =
                await _paymentService.InitiateKhaltiPaymentAsync(
                    dto.OrderId,
                    currentUserId.Value,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Khalti payment initiation failed for OrderId {OrderId}.",
                dto.OrderId);

            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // =========================================================
    // KHALTI CALLBACK
    //
    // GET:
    // api/Payments/khalti/callback?pidx=...
    // =========================================================

    [AllowAnonymous]
    [HttpGet("khalti/callback")]
    public async Task<IActionResult> KhaltiCallback(
        [FromQuery] string? pidx,
        CancellationToken cancellationToken)
    {
        // =====================================================
        // VALIDATE PIDX
        // =====================================================

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
                await _paymentService.VerifyKhaltiPaymentAsync(
                    pidx,
                    cancellationToken);

            _logger.LogInformation(
                "Khalti payment verified successfully. Pidx: {Pidx}",
                pidx);

            var mvcBaseUrl = _configuration["Mvc:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(mvcBaseUrl) &&
                (Request.Headers.Accept.ToString().Contains("text/html") || !Request.Headers.Accept.ToString().Contains("application/json")))
            {
                return RedirectToMvcSuccess(payment.OrderId, pidx);
            }

            return Ok(new
            {
                success = true,
                message = "Khalti payment verified successfully.",
                payment
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Khalti payment verification failed. Pidx: {Pidx}",
                pidx);

            var mvcBaseUrl = _configuration["Mvc:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(mvcBaseUrl) &&
                (Request.Headers.Accept.ToString().Contains("text/html") || !Request.Headers.Accept.ToString().Contains("application/json")))
            {
                return RedirectToMvcFailure(pidx, ex.Message);
            }

            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during Khalti verification. Pidx: {Pidx}",
                pidx);

            var mvcBaseUrl = _configuration["Mvc:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(mvcBaseUrl) &&
                (Request.Headers.Accept.ToString().Contains("text/html") || !Request.Headers.Accept.ToString().Contains("application/json")))
            {
                return RedirectToMvcFailure(pidx, "An unexpected error occurred while verifying your Khalti payment.");
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message = "An unexpected error occurred while verifying your Khalti payment."
                });
        }
    }


    // =========================================================
    // ESEWA INITIATE
    //
    // POST: api/Payments/esewa/initiate
    // =========================================================

    [Authorize]
    [HttpPost("esewa/initiate")]
    public async Task<IActionResult> InitiateEsewaPayment(
        [FromBody] CreateESewaPaymentDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _esewaPaymentService.InitiatePaymentAsync(
                    dto.OrderId,
                    currentUserId.Value,
                    cancellationToken);

            _logger.LogInformation(
                "eSewa payment initiated for OrderId {OrderId}, UserId {UserId}.",
                dto.OrderId,
                currentUserId.Value);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "eSewa payment initiation failed for OrderId {OrderId}.",
                dto.OrderId);

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
    // IMPORTANT:
    //
    // The callback status is NOT trusted directly.
    //
    // We always verify with the official eSewa Status API.
    // =========================================================

    [AllowAnonymous]
    [HttpGet("esewa/success")]
    public async Task<IActionResult> EsewaSuccess(
        [FromQuery] string? data,
        CancellationToken cancellationToken)
    {
        // =====================================================
        // STEP 1: VALIDATE DATA
        // =====================================================

        if (string.IsNullOrWhiteSpace(data))
        {
            return BadRequest(new
            {
                success = false,
                message = "eSewa callback data is missing."
            });
        }


        // =====================================================
        // STEP 2: DECODE BASE64 DATA
        // =====================================================

        if (!TryDecodeBase64(data, out var decodedData))
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid Base64 eSewa callback data."
            });
        }


        // =====================================================
        // STEP 3: PARSE JSON
        // =====================================================

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(decodedData);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid JSON received from eSewa callback.");

            return BadRequest(new
            {
                success = false,
                message = "Invalid eSewa callback JSON."
            });
        }


        using (document)
        {
            var root = document.RootElement;


            // =================================================
            // STEP 4: EXTRACT TRANSACTION UUID
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
                    message = "eSewa transaction UUID is missing."
                });
            }


            // =================================================
            // STEP 5: EXTRACT CALLBACK STATUS
            // =================================================

            var status =
                root.TryGetProperty(
                    "status",
                    out var statusElement)
                    ? statusElement.GetString()
                    : null;


            _logger.LogInformation(
                "eSewa success callback received. TransactionUuid: {TransactionUuid}, Status: {Status}",
                transactionUuid,
                status);


            // =================================================
            // STEP 6: CHECK CALLBACK STATUS
            //
            // IMPORTANT:
            //
            // COMPLETE alone is NOT proof of payment.
            //
            // But if it isn't COMPLETE, we immediately redirect
            // to the MVC failure page.
            // =================================================

            if (!string.Equals(
                    status,
                    "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "eSewa callback returned non-complete status. TransactionUuid: {TransactionUuid}, Status: {Status}",
                    transactionUuid,
                    status);

                return RedirectToMvcFailure(
                    transactionUuid,
                    $"eSewa payment status: {status ?? "UNKNOWN"}");
            }


            // =================================================
            // STEP 7: FIND PAYMENT
            // =================================================

            var payment =
                await _paymentService.GetPaymentByTransactionUuidAsync(
                    transactionUuid,
                    cancellationToken);


            if (payment == null)
            {
                _logger.LogWarning(
                    "Payment not found for eSewa transaction UUID {TransactionUuid}.",
                    transactionUuid);

                return RedirectToMvcFailure(
                    transactionUuid,
                    "Payment was not found.");
            }


            // =================================================
            // STEP 8: GET ORDER ID
            // =================================================

            var orderId = payment.OrderId;


            // =================================================
            // STEP 9: SERVER-SIDE VERIFICATION
            //
            // EsewaPaymentService.VerifyPaymentAsync:
            //
            // ✓ Calls official eSewa Status API
            // ✓ Validates status
            // ✓ Validates transaction UUID
            // ✓ Validates amount
            // ✓ Updates Payment -> Paid
            // ✓ Updates Order -> Confirmed
            // =================================================

            bool verified;

            try
            {
                verified =
                    await _esewaPaymentService.VerifyPaymentAsync(
                        orderId,
                        transactionUuid,
                        cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "Exception during eSewa verification. OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                    orderId,
                    transactionUuid);

                return RedirectToMvcFailure(
                    transactionUuid,
                    ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during eSewa verification. OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                    orderId,
                    transactionUuid);

                return RedirectToMvcFailure(
                    transactionUuid,
                    "An unexpected error occurred while verifying your payment.");
            }


            // =================================================
            // STEP 10: VERIFICATION FAILED
            // =================================================

            if (!verified)
            {
                _logger.LogWarning(
                    "eSewa verification failed. OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                    orderId,
                    transactionUuid);

                return RedirectToMvcFailure(
                    transactionUuid,
                    "eSewa payment verification failed.");
            }


            // =================================================
            // STEP 11: PAYMENT VERIFIED SUCCESSFULLY
            // =================================================

            _logger.LogInformation(
                "eSewa payment verified successfully. OrderId: {OrderId}, TransactionUuid: {TransactionUuid}",
                orderId,
                transactionUuid);


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
    // api/Payments/esewa/failure?data=BASE64_DATA
    // =========================================================

    [AllowAnonymous]
    [HttpGet("esewa/failure")]
    public IActionResult EsewaFailure(
        [FromQuery] string? data)
    {
        // =====================================================
        // IMPORTANT
        //
        // eSewa may redirect to failure_url without callback
        // data when:
        //
        // - User cancels payment
        // - Payment fails
        // - Payment is abandoned
        //
        // Missing data should NOT display an API error page.
        //
        // Redirect the user back to the MVC application.
        // =====================================================

        if (string.IsNullOrWhiteSpace(data))
        {
            _logger.LogWarning(
                "eSewa failure callback received without callback data.");

            return RedirectToMvcFailure(
                null,
                "Your eSewa payment was cancelled or could not be completed.");
        }


        string? transactionUuid = null;


        // =====================================================
        // DECODE BASE64 DATA
        // =====================================================

        if (!TryDecodeBase64(data, out var decodedData))
        {
            _logger.LogWarning(
                "Invalid Base64 data received from eSewa failure callback.");

            return RedirectToMvcFailure(
                null,
                "Your eSewa payment could not be completed.");
        }


        // =====================================================
        // PARSE JSON
        // =====================================================

        try
        {
            using var document =
                JsonDocument.Parse(decodedData);

            var root = document.RootElement;


            // =================================================
            // TRANSACTION UUID
            // =================================================

            if (root.TryGetProperty(
                    "transaction_uuid",
                    out var transactionUuidElement))
            {
                transactionUuid =
                    transactionUuidElement.GetString();
            }


            // =================================================
            // STATUS
            // =================================================

            var status =
                root.TryGetProperty(
                    "status",
                    out var statusElement)
                    ? statusElement.GetString()
                    : null;


            _logger.LogWarning(
                "eSewa failure callback received. " +
                "TransactionUuid: {TransactionUuid}, Status: {Status}",
                transactionUuid,
                status);


            // =================================================
            // OPTIONAL:
            // MARK PAYMENT AS FAILED
            //
            // This is optional and depends on your desired
            // retry/payment persistence flow.
            // =================================================
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid JSON received from eSewa failure callback.");

            return RedirectToMvcFailure(
                null,
                "Your eSewa payment could not be completed.");
        }


        // =====================================================
        // REDIRECT CUSTOMER TO MVC
        // =====================================================

        return RedirectToMvcFailure(
            transactionUuid,
            "eSewa payment was not completed.");
    }


    // =========================================================
    // GET CURRENT USER ID
    // =========================================================

    private int? GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (int.TryParse(
                userId,
                out var currentUserId))
        {
            return currentUserId;
        }

        return null;
    }


    // =========================================================
    // BASE64 DECODER
    //
    // Supports normal Base64 and URL-safe Base64.
    // =========================================================

    private static bool TryDecodeBase64(
        string data,
        out string decodedData)
    {
        decodedData = string.Empty;

        try
        {
            // =================================================
            // URL-SAFE BASE64 SUPPORT
            // =================================================

            var normalizedData =
                data
                    .Replace('-', '+')
                    .Replace('_', '/');


            // =================================================
            // RESTORE PADDING
            // =================================================

            switch (normalizedData.Length % 4)
            {
                case 2:
                    normalizedData += "==";
                    break;

                case 3:
                    normalizedData += "=";
                    break;
            }


            var decodedBytes =
                Convert.FromBase64String(
                    normalizedData);

            decodedData =
                Encoding.UTF8.GetString(
                    decodedBytes);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }


    // =========================================================
    // MVC SUCCESS REDIRECT
    //
    // Example:
    //
    // https://your-mvc-site.com/Payment/Success
    //     ?orderId=210001
    //     &transactionUuid=xxxx
    // =========================================================

    private IActionResult RedirectToMvcSuccess(
        int orderId,
        string transactionUuid)
    {
        var mvcBaseUrl =
            _configuration["Mvc:BaseUrl"];


        if (string.IsNullOrWhiteSpace(mvcBaseUrl))
        {
            _logger.LogError(
                "Mvc:BaseUrl configuration is missing.");

            return Problem(
                "MVC application URL is not configured.",
                statusCode:
                    StatusCodes.Status500InternalServerError);
        }


        var mvcUrl =
            $"{mvcBaseUrl.TrimEnd('/')}/Payment/Success";


        var redirectUrl =
            $"{mvcUrl}" +
            $"?orderId={Uri.EscapeDataString(orderId.ToString())}" +
            $"&transactionUuid={Uri.EscapeDataString(transactionUuid)}";


        _logger.LogInformation(
            "Redirecting successful eSewa payment to MVC. URL: {RedirectUrl}",
            redirectUrl);


        return Redirect(redirectUrl);
    }


    // =========================================================
    // MVC FAILURE REDIRECT
    //
    // Example:
    //
    // https://your-mvc-site.com/Payment/Failure
    //     ?message=...
    //     &transactionUuid=...
    // =========================================================

    private IActionResult RedirectToMvcFailure(
        string? transactionUuid,
        string message)
    {
        var mvcBaseUrl =
            _configuration["Mvc:BaseUrl"];


        if (string.IsNullOrWhiteSpace(mvcBaseUrl))
        {
            _logger.LogError(
                "Mvc:BaseUrl configuration is missing.");

            return Problem(
                "MVC application URL is not configured.",
                statusCode:
                    StatusCodes.Status500InternalServerError);
        }


        var mvcUrl =
            $"{mvcBaseUrl.TrimEnd('/')}/Payment/Failure";


        var redirectUrl =
            $"{mvcUrl}" +
            $"?message={Uri.EscapeDataString(message)}";


        if (!string.IsNullOrWhiteSpace(transactionUuid))
        {
            redirectUrl +=
                $"&transactionUuid=" +
                $"{Uri.EscapeDataString(transactionUuid)}";
        }


        _logger.LogInformation(
            "Redirecting failed eSewa payment to MVC. TransactionUuid: {TransactionUuid}",
            transactionUuid);


        return Redirect(redirectUrl);
    }
}