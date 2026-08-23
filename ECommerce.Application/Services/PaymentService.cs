using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IKhaltiPaymentService _khaltiPaymentService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IKhaltiPaymentService khaltiPaymentService,
    ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _khaltiPaymentService = khaltiPaymentService;
        _logger = logger;
    }

    public async Task<PaymentDto> CreatePaymentAsync(
        CreatePaymentDto dto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            dto.OrderId,
            userId,
            cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException(
                "Order not found.");
        }

        var existingPayment =
            await _paymentRepository.GetByOrderIdAsync(
                dto.OrderId,
                cancellationToken);

        if (existingPayment != null)
        {
            throw new InvalidOperationException(
                "Payment already exists for this order.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cannot make payment for a cancelled order.");
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            PaymentDate = DateTime.UtcNow
        };

        if (dto.Method == PaymentMethod.CashOnDelivery)
        {
            payment.Status = PaymentStatus.Pending;
            order.Status = OrderStatus.Confirmed;
        }

        await _paymentRepository.CreateAsync(
            payment,
            cancellationToken);

        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
    "Payment {PaymentId} created for OrderId {OrderId}. Method: {PaymentMethod}, Amount: {Amount}",
    payment.Id,
    payment.OrderId,
    payment.Method,
    payment.Amount);

        return MapToDto(payment);
    }

    public async Task<PaymentDto?> GetPaymentByOrderIdAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            userId,
            cancellationToken);

        if (order == null)
        {
            return null;
        }

        var payment =
            await _paymentRepository.GetByOrderIdAsync(
                orderId,
                cancellationToken);

        if (payment == null)
        {
            return null;
        }

        return MapToDto(payment);
    }

    public async Task<KhaltiPaymentInitiationDto>
        InitiateKhaltiPaymentAsync(
            int orderId,
            int userId,
            CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            userId,
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

        var existingPayment =
            await _paymentRepository.GetByOrderIdAsync(
                orderId,
                cancellationToken);

        if (existingPayment != null)
        {
            throw new InvalidOperationException(
                "Payment already exists for this order.");
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = PaymentMethod.Khalti,
            Status = PaymentStatus.Pending,
            PaymentDate = DateTime.UtcNow
        };

        await _paymentRepository.CreateAsync(
            payment,
            cancellationToken);

        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        try
        {
            var khaltiResult =
                await _khaltiPaymentService.InitiatePaymentAsync(
                    order.Id,
                    order.TotalAmount,
                    cancellationToken);

            _logger.LogInformation(
    "Khalti payment initiated for OrderId {OrderId}. Pidx: {Pidx}",
    order.Id,
    khaltiResult.Pidx);

            return new KhaltiPaymentInitiationDto
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Pidx = khaltiResult.Pidx,
                PaymentUrl = khaltiResult.PaymentUrl
            };
        }
        catch
        {
            payment.Status = PaymentStatus.Failed;

            await _paymentRepository.SaveChangesAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<PaymentDto> VerifyKhaltiPaymentAsync(
        string pidx,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pidx))
        {
            throw new InvalidOperationException(
                "Khalti payment identifier is required.");
        }

        var khaltiResult =
            await _khaltiPaymentService.VerifyPaymentAsync(
                pidx,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(
                khaltiResult.PurchaseOrderId))
        {
            throw new InvalidOperationException(
                "Khalti verification did not return an order ID.");
        }

        if (!int.TryParse(
                khaltiResult.PurchaseOrderId,
                out var orderId))
        {
            throw new InvalidOperationException(
                "Invalid Khalti purchase order ID.");
        }

        var payment =
            await _paymentRepository.GetByOrderIdAsync(
                orderId,
                cancellationToken);

        if (payment == null)
        {
            throw new InvalidOperationException(
                "Payment not found for this order.");
        }

        if (payment.Method != PaymentMethod.Khalti)
        {
            throw new InvalidOperationException(
                "This order is not using Khalti payment.");
        }

        var expectedAmountInPaisa =
            checked(
                (long)Math.Round(
                    payment.Amount * 100m,
                    MidpointRounding.AwayFromZero));

        if (khaltiResult.TotalAmount != expectedAmountInPaisa)
        {
            payment.Status = PaymentStatus.Failed;

            await _paymentRepository.SaveChangesAsync(
                cancellationToken);

            throw new InvalidOperationException(
                "Khalti payment amount does not match the order amount.");
        }

        if (!string.Equals(
                khaltiResult.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase))
            
        {
            _logger.LogWarning(
"Khalti payment was not completed for OrderId {OrderId}. Status: {Status}",
payment.OrderId,
khaltiResult.Status);
            payment.Status = PaymentStatus.Failed;

            await _paymentRepository.SaveChangesAsync(
                cancellationToken);

            throw new InvalidOperationException(
                $"Khalti payment was not completed. " +
                $"Current status: {khaltiResult.Status}");
        }

        payment.Status = PaymentStatus.Paid;
        payment.TransactionId = khaltiResult.TransactionId;

        _logger.LogInformation(
    "Khalti payment completed successfully for OrderId {OrderId}. TransactionId: {TransactionId}",
    payment.OrderId,
    payment.TransactionId);

        var order = await _orderRepository.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException(
                "Order not found for this payment.");
        }

        if (order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Confirmed;
        }

        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            PaymentDate = payment.PaymentDate
        };
    }
}

