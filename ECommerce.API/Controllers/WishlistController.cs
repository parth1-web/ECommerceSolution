using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(
        IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<ActionResult<WishlistDto>> GetWishlist(
        CancellationToken cancellationToken)
    {
        var wishlist =
            await _wishlistService.GetWishlistAsync(
                cancellationToken);

        return Ok(wishlist);
    }

    [HttpPost("items")]
    public async Task<ActionResult<WishlistDto>> AddItem(
        [FromBody] AddWishlistItemDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var wishlist =
                await _wishlistService.AddItemAsync(
                    dto,
                    cancellationToken);

            return Ok(wishlist);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<WishlistDto>> RemoveItem(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var wishlist =
                await _wishlistService.RemoveItemAsync(
                    productId,
                    cancellationToken);

            return Ok(wishlist);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearWishlist(
        CancellationToken cancellationToken)
    {
        await _wishlistService.ClearWishlistAsync(
            cancellationToken);

        return NoContent();
    }
}