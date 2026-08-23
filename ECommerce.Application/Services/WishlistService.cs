using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly ICurrentUserService _currentUserService;

    public WishlistService(
        IWishlistRepository wishlistRepository,
        ICurrentUserService currentUserService)
    {
        _wishlistRepository = wishlistRepository;
        _currentUserService = currentUserService;
    }

    public async Task<WishlistDto> GetWishlistAsync(
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var wishlist =
            await _wishlistRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (wishlist == null)
        {
            wishlist = await CreateWishlistAsync(
                userId,
                cancellationToken);

            wishlist =
                await _wishlistRepository.GetByUserIdAsync(
                    userId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Unable to create wishlist.");
        }

        return MapToDto(wishlist);
    }

    public async Task<WishlistDto> AddItemAsync(
        AddWishlistItemDto dto,
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var product =
            await _wishlistRepository.GetProductAsync(
                dto.ProductId,
                cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException(
                "Product not found.");
        }

        if (!product.IsActive)
        {
            throw new InvalidOperationException(
                "This product is not available.");
        }

        var wishlist =
            await _wishlistRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (wishlist == null)
        {
            wishlist = await CreateWishlistAsync(
                userId,
                cancellationToken);

            wishlist =
                await _wishlistRepository.GetByUserIdAsync(
                    userId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Unable to create wishlist.");
        }

        var existingItem = wishlist.WishlistItems
            .FirstOrDefault(
                wi => wi.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            return MapToDto(wishlist);
        }

        var wishlistItem = new WishlistItem
        {
            WishlistId = wishlist.Id,
            ProductId = product.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _wishlistRepository.AddItemAsync(
            wishlistItem,
            cancellationToken);

        wishlist.WishlistItems.Add(wishlistItem);

        wishlist.UpdatedAt = DateTime.UtcNow;

        await _wishlistRepository.SaveChangesAsync(
            cancellationToken);

        return await GetWishlistAsync(
            cancellationToken);
    }

    public async Task<WishlistDto> RemoveItemAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var wishlist =
            await _wishlistRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (wishlist == null)
        {
            throw new KeyNotFoundException(
                "Wishlist not found.");
        }

        var item = wishlist.WishlistItems
            .FirstOrDefault(
                wi => wi.ProductId == productId);

        if (item == null)
        {
            throw new KeyNotFoundException(
                "Product is not in the wishlist.");
        }

        _wishlistRepository.RemoveItem(item);

        wishlist.UpdatedAt = DateTime.UtcNow;

        await _wishlistRepository.SaveChangesAsync(
            cancellationToken);

        return await GetWishlistAsync(
            cancellationToken);
    }

    public async Task ClearWishlistAsync(
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var wishlist =
            await _wishlistRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (wishlist == null)
        {
            return;
        }

        _wishlistRepository.RemoveWishlist(wishlist);

        await _wishlistRepository.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Wishlist> CreateWishlistAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var wishlist = new Wishlist
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _wishlistRepository.CreateAsync(
            wishlist,
            cancellationToken);

        await _wishlistRepository.SaveChangesAsync(
            cancellationToken);

        return wishlist;
    }

    private int GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User must be authenticated.");
        }

        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Current user ID could not be determined.");
        }

        return _currentUserService.UserId.Value;
    }

    private static WishlistDto MapToDto(
        Wishlist wishlist)
    {
        var items = wishlist.WishlistItems
            .Where(wi => wi.Product != null)
            .Select(wi => new WishlistItemDto
            {
                Id = wi.Id,
                ProductId = wi.ProductId,
                ProductName = wi.Product!.Name,
                Description = wi.Product.Description,
                Price = wi.Product.Price,
                ImageUrl = wi.Product.ImageUrl,
                IsActive = wi.Product.IsActive,
                Stock = wi.Product.Stock,
                AddedAt = wi.CreatedAt
            })
            .ToList();

        return new WishlistDto
        {
            Id = wishlist.Id,
            UserId = wishlist.UserId,
            Items = items,
            TotalItems = items.Count
        };
    }
}