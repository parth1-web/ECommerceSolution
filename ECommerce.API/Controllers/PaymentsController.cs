
using System.Security.Claims;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    // POST: api/Payments
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> CreatePayment(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user identity.");
        }
            var payment =
                await _paymentService.CreatePaymentAsync(
                    dto,
                    userId,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetPaymentByOrderId),
                new { orderId = payment.OrderId },
                payment);
        }
        

    // GET: api/Payments/order/{orderId}
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<PaymentDto>>
        GetPaymentByOrderId(
            int orderId,
            CancellationToken cancellationToken)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user identity.");
        }

        var payment =
            await _paymentService.GetPaymentByOrderIdAsync(
                orderId,
                userId,
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

    // POST: api/Payments/khalti/initiate
    [HttpPost("khalti/initiate")]
    public async Task<ActionResult<KhaltiPaymentInitiationDto>>
        InitiateKhaltiPayment(
            [FromBody] CreatePaymentDto dto,
            CancellationToken cancellationToken)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user identity.");
        }

        if (dto.Method != Domain.Enums.PaymentMethod.Khalti)
        {
            return BadRequest(new
            {
                message =
                    "Payment method must be Khalti."
            });
        }

            var result =
                await _paymentService.InitiateKhaltiPaymentAsync(
                    dto.OrderId,
                    userId,
                    cancellationToken);

            return Ok(result);
        }   

    // GET: api/Payments/khalti/callback
    [AllowAnonymous]
    [HttpGet("khalti/callback")]
    public async Task<IActionResult> KhaltiCallback(
        [FromQuery] string? pidx,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pidx))
        {
            return BadRequest(new
            {
                message = "Khalti payment identifier is missing."
            });
        }

        try
        {
            var payment =
                await _paymentService.VerifyKhaltiPaymentAsync(
                    pidx,
                    cancellationToken);

            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
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

    [AllowAnonymous]
    [HttpGet("esewa/success")]
    public async Task<IActionResult> EsewaSuccess(
    [FromQuery] int orderId,
    [FromQuery] string transactionUuid,
    CancellationToken cancellationToken)
    {
        var success =
            await _esewaPaymentService.VerifyPaymentAsync(
                orderId,
                transactionUuid,
                cancellationToken);

        if (!success)
        {
            return BadRequest(new
            {
                message = "eSewa payment verification failed."
            });
        }

        return Ok(new
        {
            message = "eSewa payment verified successfully."
        });
    }

    [AllowAnonymous]
    [HttpGet("esewa/failure")]
    public IActionResult EsewaFailure()
    {
        return BadRequest(new
        {
            message = "eSewa payment was cancelled or failed."
        });
    }
    
}

