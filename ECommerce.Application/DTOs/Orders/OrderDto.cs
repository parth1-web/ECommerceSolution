using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public PaymentDto? Payment { get; set; }

    public List<OrderItemDto> Items { get; set; }
        = new();
}