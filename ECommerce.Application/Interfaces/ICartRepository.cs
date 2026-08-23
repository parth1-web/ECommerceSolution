using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<CartItem?> GetItemAsync(
        int cartId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<Cart> CreateAsync(
        Cart cart,
        CancellationToken cancellationToken = default);

    Task AddItemAsync(
        CartItem cartItem,
        CancellationToken cancellationToken = default);

    void RemoveItem(CartItem cartItem);

    void RemoveCart(Cart cart);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}