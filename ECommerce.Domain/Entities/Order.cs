using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();

    public Payment? Payment { get; set; }
}