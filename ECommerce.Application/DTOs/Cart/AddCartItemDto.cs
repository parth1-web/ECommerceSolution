using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Cart;

public class AddCartItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}