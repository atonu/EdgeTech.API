using System.Security.Claims;
using System.Text.Json;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
    {
        var cartItems = await _db.CartItems
            .Include(ci => ci.Product).ThenInclude(p => p.Images)
            .Where(ci => ci.UserId == UserId).ToListAsync();

        if (!cartItems.Any()) return BadRequest(new { message = "Cart is empty" });

        var order = new Order
        {
            UserId = UserId,
            Status = OrderStatus.Pending,
            ShippingAddress = JsonSerializer.Serialize(req.ShippingAddress),
            Notes = req.Notes,
            PaymentMethod = req.PaymentMethod,
            TotalAmount = cartItems.Sum(ci => (ci.Product.DiscountPrice ?? ci.Product.Price) * ci.Quantity)
        };

        foreach (var ci in cartItems)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.Product.DiscountPrice ?? ci.Product.Price,
                ProductSnapshot = JsonSerializer.Serialize(new { ci.Product.Name, ci.Product.SKU })
            });
        }

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cartItems);
        await _db.SaveChangesAsync();
        return Ok(new { orderId = order.Id });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        var orders = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(orders.Select(MapToDto));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetOrder(int id)
    {
        var isAdmin = User.IsInRole("Admin");
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(o => o.Id == id && (isAdmin || o.UserId == UserId));
        if (order == null) return NotFound();
        return Ok(MapToDto(order));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var total = await _db.Orders.CountAsync();
        var orders = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<OrderDto>(orders.Select(MapToDto).ToList(), total, page, pageSize, (int)Math.Ceiling((double)total / pageSize)));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = req.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { status = order.Status.ToString() });
    }

    private static OrderDto MapToDto(Order o)
    {
        var address = JsonSerializer.Deserialize<ShippingAddressDto>(o.ShippingAddress) ??
            new ShippingAddressDto("", "", "", "", "", "", "");
        return new OrderDto(o.Id, o.Status, o.TotalAmount, address, o.Notes, o.PaymentMethod, o.CreatedAt,
            o.Items.Select(i => new OrderItemDto(
                i.Id, i.ProductId, i.Product.Name,
                i.Product.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
                i.UnitPrice, i.Quantity
            )).ToList());
    }
}
