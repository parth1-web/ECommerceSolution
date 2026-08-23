namespace ECommerce.Application.DTOs.Cart;

public class CartDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public List<CartItemDto> Items { get; set; } = new();

    public int TotalItems { get; set; }

    public decimal TotalAmount { get; set; }
}