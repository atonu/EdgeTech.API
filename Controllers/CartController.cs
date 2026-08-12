using System.Security.Claims;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;
    public CartController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var items = await _db.CartItems.Find(ci => ci.UserId == UserId).ToListAsync();
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        var dtos = items.Select(ci => new CartItemDto(
            ci.Id,
            ci.ProductId,
            productMap.GetValueOrDefault(ci.ProductId)?.Name ?? "Unknown Product",
            productMap.GetValueOrDefault(ci.ProductId)?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? productMap.GetValueOrDefault(ci.ProductId)?.Images.FirstOrDefault()?.ImageUrl,
            productMap.GetValueOrDefault(ci.ProductId)?.Price ?? 0,
            productMap.GetValueOrDefault(ci.ProductId)?.DiscountPrice,
            ci.Quantity,
            productMap.GetValueOrDefault(ci.ProductId)?.Stock ?? 0
        )).Where(i => i.ProductId > 0).ToList();

        return Ok(new CartDto(dtos, dtos.Sum(i => (i.DiscountPrice ?? i.Price) * i.Quantity)));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
    {
        if (req.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero" });

        var product = await _db.Products.Find(p => p.Id == req.ProductId).FirstOrDefaultAsync();
        if (product == null || !product.IsActive) return NotFound();

        var existing = await _db.CartItems.Find(ci => ci.UserId == UserId && ci.ProductId == req.ProductId).FirstOrDefaultAsync();
        var newQuantity = (existing?.Quantity ?? 0) + req.Quantity;
        if (newQuantity > product.Stock)
            return BadRequest(new { message = "Requested quantity exceeds available stock", availableStock = product.Stock });

        if (existing != null)
        {
            await _db.CartItems.UpdateOneAsync(ci => ci.Id == existing.Id, Builders<CartItem>.Update.Set(ci => ci.Quantity, newQuantity));
        }
        else
        {
            var item = new CartItem
            {
                Id = await _ids.NextAsync("cartItems"),
                UserId = UserId,
                ProductId = req.ProductId,
                Quantity = req.Quantity
            };
            await _db.CartItems.InsertOneAsync(item);
        }
        return Ok();
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest req)
    {
        var item = await _db.CartItems.Find(ci => ci.Id == id && ci.UserId == UserId).FirstOrDefaultAsync();

        if (item == null) return NotFound();

        if (req.Quantity <= 0)
        {
            await _db.CartItems.DeleteOneAsync(ci => ci.Id == id && ci.UserId == UserId);
            return Ok();
        }

        var product = await _db.Products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
        if (product == null || !product.IsActive)
            return NotFound();

        if (req.Quantity > product.Stock)
            return BadRequest(new { message = "Requested quantity exceeds available stock", availableStock = product.Stock });

        await _db.CartItems.UpdateOneAsync(ci => ci.Id == id && ci.UserId == UserId, Builders<CartItem>.Update.Set(ci => ci.Quantity, req.Quantity));

        return Ok();
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> RemoveCartItem(int id)
    {
        var result = await _db.CartItems.DeleteOneAsync(ci => ci.Id == id && ci.UserId == UserId);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _db.CartItems.DeleteManyAsync(ci => ci.UserId == UserId);
        return NoContent();
    }
}
