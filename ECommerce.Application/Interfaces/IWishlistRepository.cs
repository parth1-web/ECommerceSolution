using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<WishlistItem?> GetItemAsync(
        int wishlistId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<Wishlist> CreateAsync(
        Wishlist wishlist,
        CancellationToken cancellationToken = default);

    Task AddItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken = default);

    void RemoveItem(WishlistItem item);

    void RemoveWishlist(Wishlist wishlist);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}