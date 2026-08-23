namespace ECommerce.Domain.Entities;

public class Cart
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public ICollection<CartItem> CartItems { get; set; }
        = new List<CartItem>();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}