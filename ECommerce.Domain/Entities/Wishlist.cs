namespace ECommerce.Domain.Entities;

public class Wishlist
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public ICollection<WishlistItem> WishlistItems { get; set; }
        = new List<WishlistItem>();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}