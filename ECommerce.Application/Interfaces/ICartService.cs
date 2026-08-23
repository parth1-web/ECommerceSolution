using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(
        CancellationToken cancellationToken = default);

    Task<CartDto> AddItemAsync(
        AddCartItemDto dto,
        CancellationToken cancellationToken = default);

    Task<CartDto> UpdateItemAsync(
        int productId,
        UpdateCartItemDto dto,
        CancellationToken cancellationToken = default);

    Task<CartDto> RemoveItemAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task ClearCartAsync(
        CancellationToken cancellationToken = default);
}