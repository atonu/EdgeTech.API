using System.Security.Claims;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Controllers;

// Recently Viewed
[ApiController]
[Route("api/recently-viewed")]
[Authorize]
public class RecentlyViewedController : ControllerBase
{
    private readonly AppDbContext _db;
    public RecentlyViewedController(AppDbContext db) => _db = db;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("{productId}")]
    public async Task<IActionResult> Track(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();

        var existing = await _db.RecentlyViewed.FirstOrDefaultAsync(r => r.UserId == UserId && r.ProductId == productId);
        if (existing != null) { existing.ViewedAt = DateTime.UtcNow; }
        else { _db.RecentlyViewed.Add(new RecentlyViewed { UserId = UserId, ProductId = productId }); }

        // Keep only last 10
        var old = await _db.RecentlyViewed.Where(r => r.UserId == UserId)
            .OrderByDescending(r => r.ViewedAt).Skip(10).ToListAsync();
        _db.RecentlyViewed.RemoveRange(old);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var items = await _db.RecentlyViewed
            .Include(r => r.Product).ThenInclude(p => p.Images)
            .Include(r => r.Product).ThenInclude(p => p.Category)
            .Include(r => r.Product).ThenInclude(p => p.Brand)
            .Where(r => r.UserId == UserId)
            .OrderByDescending(r => r.ViewedAt)
            .Take(6)
            .ToListAsync();

        return Ok(items.Select(r => new ProductListDto(
            r.Product.Id, r.Product.Name, r.Product.Slug,
            r.Product.Price, r.Product.DiscountPrice,
            r.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? r.Product.Images.FirstOrDefault()?.ImageUrl,
            r.Product.Stock, r.Product.IsFeatured,
            r.Product.Category.Name, r.Product.Brand.Name
        )));
    }
}

// Admin Users
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    public AdminUsersController(UserManager<ApplicationUser> um) => _userManager = um;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = _userManager.Users.ToList();
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, Role = roles.FirstOrDefault() ?? "User", u.CreatedAt });
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        var user = new ApplicationUser
        {
            UserName = req.Email, Email = req.Email,
            FirstName = req.FirstName, LastName = req.LastName, EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        await _userManager.AddToRoleAsync(user, req.Role);
        return Ok(new { user.Id, user.Email });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest req)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, req.Role);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        await _userManager.DeleteAsync(user);
        return NoContent();
    }
}

// Upload
[ApiController]
[Route("api/upload")]
[Authorize(Roles = "Admin")]
public class UploadController : ControllerBase
{
    private readonly IBlobStorageService _blob;
    public UploadController(IBlobStorageService blob) => _blob = blob;

    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "general")
    {
        if (file == null || file.Length == 0) return BadRequest("No file");
        var url = await _blob.UploadAsync(file, "edgetech-products", folder);
        return Ok(new UploadResponse(url));
    }
}
