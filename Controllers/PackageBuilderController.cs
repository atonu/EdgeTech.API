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
[Route("api/package-builder")]
public class PackageBuilderController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public PackageBuilderController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

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
        var categories = await _db.Categories.Find(c => c.IsActive).ToListAsync();
        var brands = await _db.Brands.Find(b => b.IsActive).ToListAsync();
        var products = await _db.Products.Find(p => p.IsActive).ToListAsync();

        var categoryBySlug = categories.ToDictionary(c => c.Slug, c => c.Id);
        var categoryMap = categories.ToDictionary(c => c.Id);
        var brandMap = brands.ToDictionary(b => b.Id);

        var result = new List<object>();
        foreach (var slot in Slots)
        {
            var slotProducts = new List<ProductListDto>();
            if (categoryBySlug.TryGetValue(slot.CategorySlug, out var categoryId))
            {
                slotProducts = products
                    .Where(p => p.CategoryId == categoryId)
                    .Select(p => new ProductListDto(
                        p.Id,
                        p.Name,
                        p.Slug,
                        p.Price,
                        p.DiscountPrice,
                        p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
                        p.Stock,
                        p.IsFeatured,
                        categoryMap.GetValueOrDefault(p.CategoryId)?.Name ?? "Unknown",
                        brandMap.GetValueOrDefault(p.BrandId)?.Name ?? "Unknown"
                    ))
                    .ToList();
            }

            result.Add(new { slot, products = slotProducts });
        }

        return Ok(result);
    }

    [HttpPost("save")]
    [Authorize]
    public async Task<IActionResult> SaveBuild([FromBody] SavePackageRequest req)
    {
        if (UserId == null)
            return Unauthorized();

        var productIds = req.Components.Select(c => c.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id) && p.IsActive).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        var buildId = await _ids.NextAsync("packageBuilds");
        var build = new PackageBuild
        {
            Id = buildId,
            UserId = UserId,
            Name = req.Name,
            TotalPrice = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Components = []
        };

        foreach (var comp in req.Components)
        {
            if (!productMap.TryGetValue(comp.ProductId, out var product))
                continue;

            var component = new PackageComponent
            {
                Id = await _ids.NextAsync("packageComponents"),
                PackageBuildId = buildId,
                SlotKey = comp.SlotKey,
                ProductId = comp.ProductId,
                Quantity = comp.Quantity
            };

            build.Components.Add(component);
            build.TotalPrice += (product.DiscountPrice ?? product.Price) * comp.Quantity;
        }

        await _db.PackageBuilds.InsertOneAsync(build);
        return Ok(new { id = build.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBuild(int id)
    {
        var build = await _db.PackageBuilds.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (build == null) return NotFound();
        return Ok(await MapBuildDto(build));
    }

    [HttpGet("my-builds")]
    [Authorize]
    public async Task<IActionResult> GetMyBuilds()
    {
        if (UserId == null) return Unauthorized();

        var builds = await _db.PackageBuilds.Find(b => b.UserId == UserId)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

        var dtos = new List<PackageBuildDto>();
        foreach (var build in builds)
            dtos.Add(await MapBuildDto(build));

        return Ok(dtos);
    }

    [HttpPost("{id}/add-to-cart")]
    [Authorize]
    public async Task<IActionResult> AddBuildToCart(int id)
    {
        if (UserId == null)
            return Unauthorized();

        var build = await _db.PackageBuilds.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (build == null) return NotFound();

        var productIds = build.Components.Select(c => c.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var comp in build.Components)
        {
            if (!productMap.TryGetValue(comp.ProductId, out var product) || !product.IsActive)
                return BadRequest(new { message = $"Product ID {comp.ProductId} is inactive or missing" });

            var existing = await _db.CartItems.Find(ci => ci.UserId == UserId && ci.ProductId == comp.ProductId).FirstOrDefaultAsync();
            var nextQty = (existing?.Quantity ?? 0) + comp.Quantity;
            if (nextQty > product.Stock)
            {
                return BadRequest(new
                {
                    message = $"Product '{product.Name}' exceeds available stock",
                    requested = nextQty,
                    available = product.Stock
                });
            }

            if (existing != null)
            {
                await _db.CartItems.UpdateOneAsync(ci => ci.Id == existing.Id,
                    Builders<CartItem>.Update.Set(ci => ci.Quantity, nextQty));
            }
            else
            {
                await _db.CartItems.InsertOneAsync(new CartItem
                {
                    Id = await _ids.NextAsync("cartItems"),
                    UserId = UserId,
                    ProductId = comp.ProductId,
                    Quantity = comp.Quantity,
                    AddedAt = DateTime.UtcNow
                });
            }
        }

        return Ok();
    }

    private async Task<PackageBuildDto> MapBuildDto(PackageBuild b)
    {
        var productIds = b.Components.Select(c => c.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        return new PackageBuildDto(
            b.Id,
            b.Name,
            b.TotalPrice,
            b.CreatedAt,
            b.Components.Select(c =>
            {
                var product = productMap.GetValueOrDefault(c.ProductId);
                return new PackageComponentDto(
                    c.SlotKey,
                    c.ProductId,
                    product?.Name ?? "Unknown Product",
                    product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? product?.Images.FirstOrDefault()?.ImageUrl,
                    product?.DiscountPrice ?? product?.Price ?? 0,
                    c.Quantity
                );
            }).ToList()
        );
    }
}
