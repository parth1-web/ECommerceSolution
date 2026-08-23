using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class OrderSummaryDto
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public int ItemCount { get; set; }
}