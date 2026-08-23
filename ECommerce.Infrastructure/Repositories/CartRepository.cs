using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(
                c => c.UserId == userId,
                cancellationToken);
    }

    public async Task<CartItem?> GetItemAsync(
        int cartId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(
                ci => ci.CartId == cartId &&
                      ci.ProductId == productId,
                cancellationToken);
    }

    public async Task<Product?> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(
                p => p.Id == productId,
                cancellationToken);
    }

    public async Task<Cart> CreateAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(
            cart,
            cancellationToken);

        return cart;
    }

    public async Task AddItemAsync(
        CartItem cartItem,
        CancellationToken cancellationToken = default)
    {
        await _context.CartItems.AddAsync(
            cartItem,
            cancellationToken);
    }

    public void RemoveItem(CartItem cartItem)
    {
        _context.CartItems.Remove(cartItem);
    }

    public void RemoveCart(Cart cart)
    {
        _context.Carts.Remove(cart);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}