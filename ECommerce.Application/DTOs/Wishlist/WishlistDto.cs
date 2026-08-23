namespace ECommerce.Application.DTOs.Wishlist;

public class WishlistDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public List<WishlistItemDto> Items { get; set; }
        = new();

    public int TotalItems { get; set; }
}