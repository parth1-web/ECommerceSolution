using ECommerce.Application.DTOs.Wishlist;

namespace ECommerce.Application.Interfaces;

public interface IWishlistService
{
    Task<WishlistDto> GetWishlistAsync(
        CancellationToken cancellationToken = default);

    Task<WishlistDto> AddItemAsync(
        AddWishlistItemDto dto,
        CancellationToken cancellationToken = default);

    Task<WishlistDto> RemoveItemAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task ClearWishlistAsync(
        CancellationToken cancellationToken = default);
}