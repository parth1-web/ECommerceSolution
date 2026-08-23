using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(
            cancellationToken);

        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartService.AddItemAsync(
                dto,
                cancellationToken);

            return Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
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

    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(
        int productId,
        [FromBody] UpdateCartItemDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartService.UpdateItemAsync(
                productId,
                dto,
                cancellationToken);

            return Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
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
    public async Task<ActionResult<CartDto>> RemoveItem(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartService.RemoveItemAsync(
                productId,
                cancellationToken);

            return Ok(cart);
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
    public async Task<IActionResult> ClearCart(
        CancellationToken cancellationToken)
    {
        await _cartService.ClearCartAsync(
            cancellationToken);

        return NoContent();
    }
}