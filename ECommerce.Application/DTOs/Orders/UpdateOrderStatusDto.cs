using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}