using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: api/admin/orders
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>>
        GetAllOrders(
            CancellationToken cancellationToken)
    {
        var orders =
            await _orderService.GetAllOrdersAsync(
                cancellationToken);

        return Ok(orders);
    }


    // PUT: api/admin/orders/{orderId}/status
    [HttpPut("{orderId:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(
    int orderId,
    [FromBody] UpdateOrderStatusDto dto,
    CancellationToken cancellationToken)
    {
        try
        {
            var updated =
                await _orderService.UpdateOrderStatusAsync(
                    orderId,
                    dto.Status,
                    cancellationToken);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            return Ok(new
            {
                message = "Order status updated successfully."
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
    // GET: api/admin/orders/{orderId}
    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDto>>
        GetOrderById(
            int orderId,
            CancellationToken cancellationToken)
    {
        var order =
            await _orderService.GetAdminOrderByIdAsync(
                orderId,
                cancellationToken);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        return Ok(order);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetOrdersByStatus(
    OrderStatus status,
    CancellationToken cancellationToken)
    {
        var orders =
            await _orderService.GetOrdersByStatusAsync(
                status,
                cancellationToken);

        return Ok(orders);
    }


}