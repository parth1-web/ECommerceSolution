using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Wishlist;

public class AddWishlistItemDto
{
    [Required]
    public int ProductId { get; set; }
}