using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _context;

    public WishlistRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Wishlist?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Wishlists
            .Include(w => w.WishlistItems)
            .ThenInclude(wi => wi.Product)
            .FirstOrDefaultAsync(
                w => w.UserId == userId,
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

    public async Task<WishlistItem?> GetItemAsync(
        int wishlistId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WishlistItems
            .Include(wi => wi.Product)
            .FirstOrDefaultAsync(
                wi => wi.WishlistId == wishlistId &&
                      wi.ProductId == productId,
                cancellationToken);
    }

    public async Task<Wishlist> CreateAsync(
        Wishlist wishlist,
        CancellationToken cancellationToken = default)
    {
        await _context.Wishlists.AddAsync(
            wishlist,
            cancellationToken);

        return wishlist;
    }

    public async Task AddItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken = default)
    {
        await _context.WishlistItems.AddAsync(
            item,
            cancellationToken);
    }

    public void RemoveItem(WishlistItem item)
    {
        _context.WishlistItems.Remove(item);
    }

    public void RemoveWishlist(Wishlist wishlist)
    {
        _context.Wishlists.Remove(wishlist);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}