using System.Security.Claims;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/package-builder")]
public class PackageBuilderController : ControllerBase
{
    private readonly AppDbContext _db;
    public PackageBuilderController(AppDbContext db) => _db = db;
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static readonly List<PackageSlotDefinition> Slots =
    [
        new("camera_1", "Camera 1", "Primary CCTV Camera", "ip-cameras", "camera"),
        new("camera_2", "Camera 2", "Secondary CCTV Camera", "ip-cameras", "camera"),
        new("camera_3", "Camera 3", "Third CCTV Camera", "analog-cameras", "camera"),
        new("camera_4", "Camera 4", "Fourth CCTV Camera", "analog-cameras", "camera"),
        new("dvr", "DVR / NVR", "Digital Video Recorder or Network Video Recorder", "dvr-nvr", "server"),
        new("monitor", "Monitor", "Display Monitor", "monitor", "monitor"),
        new("storage", "Hard Drive", "Surveillance HDD for recording storage", "hdd", "hard-drive"),
        new("cable", "Cable", "BNC/Network Cabling", "cable", "cable"),
        new("power", "Power Adapter", "Power Supply Unit", "power-adapter", "zap"),
        new("ups", "UPS", "Uninterruptible Power Supply", "ups", "battery"),
    ];

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots()
    {
        var result = new List<object>();
        foreach (var slot in Slots)
        {
            var products = await _db.Products
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.Category.Slug == slot.CategorySlug)
                .Select(p => new ProductListDto(
                    p.Id, p.Name, p.Slug, p.Price, p.DiscountPrice,
                    p.Images.FirstOrDefault(i => i.IsPrimary) != null
                        ? p.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl
                        : p.Images.FirstOrDefault() != null ? p.Images.First().ImageUrl : null,
                    p.Stock, p.IsFeatured, p.Category.Name, p.Brand.Name
                ))
                .ToListAsync();

            result.Add(new { slot, products });
        }
        return Ok(result);
    }

    [HttpPost("save")]
    [Authorize]
    public async Task<IActionResult> SaveBuild([FromBody] SavePackageRequest req)
    {
        var build = new PackageBuild { UserId = UserId, Name = req.Name };
        foreach (var comp in req.Components)
        {
            var product = await _db.Products.FindAsync(comp.ProductId);
            if (product == null) continue;
            build.Components.Add(new PackageComponent { SlotKey = comp.SlotKey, ProductId = comp.ProductId, Quantity = comp.Quantity });
            build.TotalPrice += (product.DiscountPrice ?? product.Price) * comp.Quantity;
        }
        _db.PackageBuilds.Add(build);
        await _db.SaveChangesAsync();
        return Ok(new { id = build.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBuild(int id)
    {
        var build = await _db.PackageBuilds
            .Include(b => b.Components).ThenInclude(c => c.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (build == null) return NotFound();
        return Ok(MapBuildDto(build));
    }

    [HttpGet("my-builds")]
    [Authorize]
    public async Task<IActionResult> GetMyBuilds()
    {
        var builds = await _db.PackageBuilds
            .Include(b => b.Components).ThenInclude(c => c.Product).ThenInclude(p => p.Images)
            .Where(b => b.UserId == UserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(builds.Select(MapBuildDto));
    }

    [HttpPost("{id}/add-to-cart")]
    [Authorize]
    public async Task<IActionResult> AddBuildToCart(int id)
    {
        var build = await _db.PackageBuilds.Include(b => b.Components).FirstOrDefaultAsync(b => b.Id == id);
        if (build == null) return NotFound();
        foreach (var comp in build.Components)
        {
            var existing = await _db.CartItems.FirstOrDefaultAsync(ci => ci.UserId == UserId && ci.ProductId == comp.ProductId);
            if (existing != null) existing.Quantity += comp.Quantity;
            else _db.CartItems.Add(new CartItem { UserId = UserId!, ProductId = comp.ProductId, Quantity = comp.Quantity });
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    private static PackageBuildDto MapBuildDto(PackageBuild b) => new(
        b.Id, b.Name, b.TotalPrice, b.CreatedAt,
        b.Components.Select(c => new PackageComponentDto(
            c.SlotKey, c.ProductId, c.Product.Name,
            c.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? c.Product.Images.FirstOrDefault()?.ImageUrl,
            c.Product.DiscountPrice ?? c.Product.Price, c.Quantity
        )).ToList()
    );
}
