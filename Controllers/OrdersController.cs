using System.Security.Claims;
using System.Text.Json;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public OrdersController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
    {
        if (UserId == null)
            return Unauthorized();

        var cartItems = await _db.CartItems.Find(ci => ci.UserId == UserId).ToListAsync();
        if (!cartItems.Any()) return BadRequest(new { message = "Cart is empty" });

        var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        var outOfStock = cartItems
            .Where(ci => !productMap.ContainsKey(ci.ProductId)
                         || !productMap[ci.ProductId].IsActive
                         || ci.Quantity > productMap[ci.ProductId].Stock)
            .Select(ci => new
            {
                ci.ProductId,
                Name = productMap.ContainsKey(ci.ProductId) ? productMap[ci.ProductId].Name : "Unknown",
                requested = ci.Quantity,
                available = productMap.ContainsKey(ci.ProductId) ? productMap[ci.ProductId].Stock : 0,
                isActive = productMap.ContainsKey(ci.ProductId) && productMap[ci.ProductId].IsActive
            })
            .ToList();

        if (outOfStock.Any())
            return BadRequest(new { message = "Some items are out of stock or inactive", items = outOfStock });

        var orderId = await _ids.NextAsync("orders");

        var order = new Order
        {
            Id = orderId,
            UserId = UserId,
            Status = OrderStatus.Pending,
            ShippingAddress = JsonSerializer.Serialize(req.ShippingAddress),
            Notes = req.Notes,
            PaymentMethod = req.PaymentMethod,
            TotalAmount = cartItems.Sum(ci => (productMap[ci.ProductId].DiscountPrice ?? productMap[ci.ProductId].Price) * ci.Quantity),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = []
        };

        foreach (var ci in cartItems)
        {
            var product = productMap[ci.ProductId];
            order.Items.Add(new OrderItem
            {
                Id = await _ids.NextAsync("orderItems"),
                OrderId = orderId,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = product.DiscountPrice ?? product.Price,
                ProductSnapshot = JsonSerializer.Serialize(new { product.Name, product.SKU })
            });

            await _db.Products.UpdateOneAsync(p => p.Id == ci.ProductId,
                Builders<Product>.Update
                    .Inc(p => p.Stock, -ci.Quantity)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow));
        }

        await _db.Orders.InsertOneAsync(order);
        await _db.CartItems.DeleteManyAsync(ci => ci.UserId == UserId);

        return Ok(new { orderId = order.Id });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        if (UserId == null) return Unauthorized();

        var orders = await _db.Orders.Find(o => o.UserId == UserId)
            .SortByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(await MapOrdersToDtos(orders));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetOrder(int id)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var order = await _db.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null) return NotFound();
        if (!isAdmin && order.UserId != userId) return NotFound();

        var dtos = await MapOrdersToDtos([order]);
        return Ok(dtos.First());
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = (int)await _db.Orders.CountDocumentsAsync(_ => true);
        var orders = await _db.Orders.Find(_ => true)
            .SortByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var dtos = await MapOrdersToDtos(orders);
        return Ok(new PagedResult<OrderDto>(dtos, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize)));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var update = Builders<Order>.Update
            .Set(o => o.Status, req.Status)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);

        var result = await _db.Orders.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Order, Order>
        {
            ReturnDocument = ReturnDocument.After
        });

        if (result == null) return NotFound();
        return Ok(new { status = result.Status.ToString() });
    }

    private async Task<List<OrderDto>> MapOrdersToDtos(IEnumerable<Order> orders)
    {
        var orderList = orders.ToList();
        var productIds = orderList.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        return orderList.Select(o =>
        {
            var address = JsonSerializer.Deserialize<ShippingAddressDto>(o.ShippingAddress) ??
                new ShippingAddressDto("", "", "", "", "", "", "");

            return new OrderDto(
                o.Id,
                o.Status,
                o.TotalAmount,
                address,
                o.Notes,
                o.PaymentMethod,
                o.CreatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    productMap.GetValueOrDefault(i.ProductId)?.Name ?? "Unknown Product",
                    productMap.GetValueOrDefault(i.ProductId)?.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl
                        ?? productMap.GetValueOrDefault(i.ProductId)?.Images.FirstOrDefault()?.ImageUrl,
                    i.UnitPrice,
                    i.Quantity
                )).ToList()
            );
        }).ToList();
    }
}
