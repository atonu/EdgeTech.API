using System.Security.Claims;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var items = await _db.CartItems
            .Include(ci => ci.Product).ThenInclude(p => p.Images)
            .Where(ci => ci.UserId == UserId).ToListAsync();

        var dtos = items.Select(ci => new CartItemDto(
            ci.Id, ci.ProductId, ci.Product.Name,
            ci.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? ci.Product.Images.FirstOrDefault()?.ImageUrl,
            ci.Product.Price, ci.Product.DiscountPrice, ci.Quantity, ci.Product.Stock
        )).ToList();

        return Ok(new CartDto(dtos, dtos.Sum(i => (i.DiscountPrice ?? i.Price) * i.Quantity)));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
    {
        var product = await _db.Products.FindAsync(req.ProductId);
        if (product == null || !product.IsActive) return NotFound();
        var existing = await _db.CartItems.FirstOrDefaultAsync(ci => ci.UserId == UserId && ci.ProductId == req.ProductId);
        if (existing != null) existing.Quantity += req.Quantity;
        else _db.CartItems.Add(new CartItem { UserId = UserId, ProductId = req.ProductId, Quantity = req.Quantity });
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest req)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == UserId);
        if (item == null) return NotFound();
        if (req.Quantity <= 0) _db.CartItems.Remove(item);
        else item.Quantity = req.Quantity;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> RemoveCartItem(int id)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == UserId);
        if (item == null) return NotFound();
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        _db.CartItems.RemoveRange(_db.CartItems.Where(ci => ci.UserId == UserId));
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
