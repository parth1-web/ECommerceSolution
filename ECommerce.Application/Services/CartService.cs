using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUserService;

    public CartService(
        ICartRepository cartRepository,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CartDto> GetCartAsync(
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var cart = await _cartRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (cart == null)
        {
            cart = await CreateCartAsync(
                userId,
                cancellationToken);
        }

        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(
        AddCartItemDto dto,
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        if (dto.Quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        var product = await _cartRepository.GetProductAsync(
            dto.ProductId,
            cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException(
                "Product not found.");
        }

        ValidateProduct(product, dto.Quantity);

        var cart = await _cartRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (cart == null)
        {
            cart = await CreateCartAsync(
                userId,
                cancellationToken);

            // Reload because the newly created cart was not loaded
            // with navigation properties.
            cart = await _cartRepository.GetByUserIdAsync(
                userId,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "Unable to create cart.");
        }

        var existingItem = cart.CartItems
            .FirstOrDefault(ci => ci.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            int newQuantity =
                existingItem.Quantity + dto.Quantity;

            ValidateProduct(product, newQuantity);

            existingItem.Quantity = newQuantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = dto.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            await _cartRepository.AddItemAsync(
                cartItem,
                cancellationToken);

            cart.CartItems.Add(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;

        await _cartRepository.SaveChangesAsync(
            cancellationToken);

        return await GetCartAsync(cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(
        int productId,
        UpdateCartItemDto dto,
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        if (dto.Quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        var cart = await _cartRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (cart == null)
        {
            throw new KeyNotFoundException(
                "Cart not found.");
        }

        var item = cart.CartItems
            .FirstOrDefault(ci => ci.ProductId == productId);

        if (item == null)
        {
            throw new KeyNotFoundException(
                "Cart item not found.");
        }

        if (item.Product == null)
        {
            throw new InvalidOperationException(
                "Product information could not be loaded.");
        }

        ValidateProduct(
            item.Product,
            dto.Quantity);

        item.Quantity = dto.Quantity;
        item.UpdatedAt = DateTime.UtcNow;

        cart.UpdatedAt = DateTime.UtcNow;

        await _cartRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var cart = await _cartRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (cart == null)
        {
            throw new KeyNotFoundException(
                "Cart not found.");
        }

        var item = cart.CartItems
            .FirstOrDefault(ci => ci.ProductId == productId);

        if (item == null)
        {
            throw new KeyNotFoundException(
                "Cart item not found.");
        }

        _cartRepository.RemoveItem(item);

        cart.UpdatedAt = DateTime.UtcNow;

        await _cartRepository.SaveChangesAsync(
            cancellationToken);

        return await GetCartAsync(cancellationToken);
    }

    public async Task ClearCartAsync(
        CancellationToken cancellationToken = default)
    {
        int userId = GetCurrentUserId();

        var cart = await _cartRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (cart == null)
        {
            return;
        }

        _cartRepository.RemoveCart(cart);

        await _cartRepository.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Cart> CreateCartAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _cartRepository.CreateAsync(
            cart,
            cancellationToken);

        await _cartRepository.SaveChangesAsync(
            cancellationToken);

        return cart;
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

    private static void ValidateProduct(
        Product product,
        int quantity)
    {
        if (!product.IsActive)
        {
            throw new InvalidOperationException(
                "This product is not available.");
        }

        if (product.Stock <= 0)
        {
            throw new InvalidOperationException(
                "This product is currently out of stock.");
        }

        if (quantity > product.Stock)
        {
            throw new InvalidOperationException(
                $"Only {product.Stock} units are available.");
        }
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.CartItems
            .Where(ci => ci.Product != null)
            .Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product!.Name,
                ImageUrl = ci.Product.ImageUrl,
                UnitPrice = ci.Product.Price,
                Quantity = ci.Quantity,
                Subtotal = ci.Product.Price * ci.Quantity
            })
            .ToList();

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items,
            TotalItems = items.Sum(i => i.Quantity),
            TotalAmount = items.Sum(i => i.Subtotal)
        };
    }
}