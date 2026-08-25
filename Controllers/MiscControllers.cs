using System.Security.Claims;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

// Recently Viewed
[ApiController]
[Route("api/recently-viewed")]
[Authorize]
public class RecentlyViewedController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;
    public RecentlyViewedController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("{productId}")]
    public async Task<IActionResult> Track(int productId)
    {
        var product = await _db.Products.Find(p => p.Id == productId).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        var existing = await _db.RecentlyViewed.Find(r => r.UserId == UserId && r.ProductId == productId).FirstOrDefaultAsync();
        if (existing != null)
        {
            var update = Builders<RecentlyViewed>.Update.Set(r => r.ViewedAt, DateTime.UtcNow);
            await _db.RecentlyViewed.UpdateOneAsync(r => r.Id == existing.Id, update);
        }
        else
        {
            var nextId = await _ids.NextAsync("recentlyViewed");
            await _db.RecentlyViewed.InsertOneAsync(new RecentlyViewed { Id = nextId, UserId = UserId, ProductId = productId });
        }

        // Keep only last 10
        var keepIds = await _db.RecentlyViewed.Find(r => r.UserId == UserId)
            .SortByDescending(r => r.ViewedAt)
            .Limit(10)
            .Project(r => r.Id)
            .ToListAsync();
        await _db.RecentlyViewed.DeleteManyAsync(r => r.UserId == UserId && !keepIds.Contains(r.Id));

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var items = await _db.RecentlyViewed.Find(r => r.UserId == UserId)
            .SortByDescending(r => r.ViewedAt)
            .Limit(6)
            .ToListAsync();

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var categories = await _db.Categories.Find(c => true).ToListAsync();
        var brands = await _db.Brands.Find(b => true).ToListAsync();

        var productMap = products.ToDictionary(p => p.Id);
        var categoryMap = categories.ToDictionary(c => c.Id);
        var brandMap = brands.ToDictionary(b => b.Id);

        var dtos = items
            .Where(r => productMap.ContainsKey(r.ProductId))
            .Select(r =>
            {
                var product = productMap[r.ProductId];
                return new ProductListDto(
                    product.Id,
                    product.Name,
                    product.Slug,
                    product.Price,
                    product.DiscountPrice,
                    product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl,
                    product.Stock,
                    product.IsFeatured,
                    categoryMap.GetValueOrDefault(product.CategoryId)?.Name ?? "Unknown",
                    brandMap.GetValueOrDefault(product.BrandId)?.Name ?? "Unknown"
                );
            })
            .ToList();

        return Ok(dtos);
    }
}

// Admin Users
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly MongoDbContext _db;
    public AdminUsersController(MongoDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var filter = Builders<ApplicationUser>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            filter = Builders<ApplicationUser>.Filter.Or(
                Builders<ApplicationUser>.Filter.Regex(u => u.Email, searchLower),
                Builders<ApplicationUser>.Filter.Regex(u => u.FirstName, searchLower),
                Builders<ApplicationUser>.Filter.Regex(u => u.LastName, searchLower)
            );
        }

        var totalCount = (int)await _db.Users.CountDocumentsAsync(filter);
        var users = await _db.Users.Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var dtos = users.Select(u => new UserDto(u.Id, u.Email, u.FirstName, u.LastName, u.Role)).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new PagedResult<UserDto>(dtos, totalCount, page, pageSize, totalPages));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        var allowedRoles = new[] { "User", "Admin" };
        if (!allowedRoles.Contains(req.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { errors = new[] { "Invalid role" } });

        var role = req.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
        var normalizedEmail = req.Email.ToLowerInvariant();
        var existing = await _db.Users.Find(u => u.Email == normalizedEmail).FirstOrDefaultAsync();
        if (existing != null) return BadRequest(new { errors = new[] { "Email is already in use" } });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Role = role,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, req.Password);
        await _db.Users.InsertOneAsync(user);
        return Ok(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest req)
    {
        var allowedRoles = new[] { "User", "Admin" };
        if (!allowedRoles.Contains(req.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { errors = new[] { "Invalid role" } });

        var update = Builders<ApplicationUser>.Update
            .Set(u => u.FirstName, req.FirstName)
            .Set(u => u.LastName, req.LastName)
            .Set(u => u.Role, req.Role);

        var result = await _db.Users.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest req)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        await _db.Users.UpdateOneAsync(u => u.Id == id, Builders<ApplicationUser>.Update.Set(u => u.Role, req.Role));
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return NotFound();
        await _db.Users.DeleteOneAsync(u => u.Id == id);
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
