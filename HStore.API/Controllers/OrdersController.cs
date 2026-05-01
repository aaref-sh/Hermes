using HStore.API.Attributes;
using HStore.Application.DTOs;
using HStore.Application.Interfaces;
using HStore.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HStore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController(IOrderService orderService) : ControllerBaseEx
{
    [AuthorizeMiddleware(["User", "Admin"])]
    [HttpPost("preview")]
    public async Task<IActionResult> GetOrderPreview([FromBody] CreateOrderDto orderDto)
    {
        orderDto.UserId = CurrentUserId;

        var orderPreview = await orderService.GetOrderPreviewAsync(orderDto);
        return Ok(orderPreview);
    }

    [AuthorizeMiddleware(["User", "Admin"])]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
    {
        orderDto.UserId = CurrentUserId;

        if (orderDto.OrderItems.Count == 0)
        {
            return BadRequest("Order must contain at least one item.");
        }

        if (orderDto.PaymentMethod == default)
        {
            return BadRequest("Payment method is required.");
        }

        var createdOrder = await orderService.CreateOrderAsync(orderDto);

        return createdOrder != null
            ? CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder)
            : BadRequest("Failed to create order.");
    }

    [AuthorizeMiddleware(["Admin"])]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var orders = await orderService.GetAllOrdersAsync(page, pageSize);
        return Ok(orders);
    }

    [AuthorizeMiddleware(["User", "Admin"])]
    [HttpGet("user")]
    public async Task<IActionResult> GetOrdersByUser()
    {
        var orders = await orderService.GetOrdersByUserAsync(CurrentUserId);
        return Ok(orders);
    }

    [AuthorizeMiddleware(["User", "Admin", "Seller"])]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        return order != null ? Ok(order) : NotFound();
    }

    [AuthorizeMiddleware(["User", "Admin"])]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        if (order == null || (order.UserId != CurrentUserId && CurrentUserRole != "Admin"))
        {
            return Forbid();
        }

        if (await orderService.CancelOrderAsync(id))
            return NoContent();

        return BadRequest($"Failed to cancel order with ID {id}.");
    }

    [AuthorizeMiddleware(["Admin", "Seller"])]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] string newStatus, [FromQuery] string? notes)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), newStatus) || !Enum.TryParse<OrderStatus>(newStatus, out var status))
        {
            return BadRequest($"Invalid status: {newStatus}");
        }
        
        await orderService.UpdateOrderStatusAsync(id, status,notes);
        return NoContent();
    }

    [AuthorizeMiddleware(["Admin"])]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        if (await orderService.DeleteOrderAsync(id))
        {
            return NoContent();
        }

        return BadRequest($"Failed to delete order with ID {id}.");
    }

    [AuthorizeMiddleware(["Admin", "Seller"])]
    [HttpPost("{id:int}/collect-cod")]
    public async Task<IActionResult> CollectCodPayment(int id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound($"Order with ID {id} not found.");
        }

        if (order.PaymentMethod != PaymentMethodType.PayOnDelivery)
        {
            return BadRequest("This order is not paid on delivery.");
        }

        if (order.OrderStatus != OrderStatus.Shipped && order.OrderStatus != OrderStatus.Delivered)
        {
            return BadRequest("COD payment can only be collected after the order is shipped or delivered.");
        }

        await orderService.UpdateOrderStatusAsync(id, OrderStatus.Paid, "COD payment collected.");
        return Ok(new { Message = "COD payment collected successfully." });
    }
}