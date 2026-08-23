using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Cart;

public class UpdateCartItemDto
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}