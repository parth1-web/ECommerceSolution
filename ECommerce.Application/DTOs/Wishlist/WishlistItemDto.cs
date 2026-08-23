namespace ECommerce.Application.DTOs.Wishlist;

public class WishlistItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int Stock { get; set; }

    public DateTime AddedAt { get; set; }
}