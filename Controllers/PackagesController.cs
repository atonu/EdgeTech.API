using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

// Public storefront endpoints for admin-authored bundle packages.
[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public PackagesController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var packages = await _db.Packages.Find(p => p.IsActive)
            .SortByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return Ok(await PackageMapper.MapManyAsync(_db, packages));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var package = await _db.Packages.Find(p => p.Id == id && p.IsActive).FirstOrDefaultAsync();
        if (package == null) return NotFound();

        var dto = (await PackageMapper.MapManyAsync(_db, [package])).First();
        return Ok(dto);
    }
}

[ApiController]
[Route("api/admin/packages")]
[Authorize(Roles = "Admin")]
public class AdminPackagesController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public AdminPackagesController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var packages = await _db.Packages.Find(_ => true)
            .SortByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return Ok(await PackageMapper.MapManyAsync(_db, packages));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePackageRequest req)
    {
        var items = await BuildItemsAsync(req.Items);
        if (items.Count == 0)
            return BadRequest(new { message = "A package must contain at least one valid product." });

        var package = new Package
        {
            Id = await _ids.NextAsync("packages"),
            Name = req.Name,
            Description = req.Description,
            ImageUrl = req.ImageUrl,
            IsActive = req.IsActive,
            IsFeatured = req.IsFeatured,
            PackagePrice = req.PackagePrice,
            // Honour an admin-set regular price; fall back to the summed component prices.
            RegularPrice = req.RegularPrice > 0 ? req.RegularPrice : await ComputeRegularPriceAsync(items),
            Items = items,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _db.Packages.InsertOneAsync(package);
        var dto = (await PackageMapper.MapManyAsync(_db, [package])).First();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePackageRequest req)
    {
        var items = await BuildItemsAsync(req.Items);
        if (items.Count == 0)
            return BadRequest(new { message = "A package must contain at least one valid product." });

        var update = Builders<Package>.Update
            .Set(p => p.Name, req.Name)
            .Set(p => p.Description, req.Description)
            .Set(p => p.ImageUrl, req.ImageUrl)
            .Set(p => p.IsActive, req.IsActive)
            .Set(p => p.IsFeatured, req.IsFeatured)
            .Set(p => p.PackagePrice, req.PackagePrice)
            .Set(p => p.RegularPrice, req.RegularPrice > 0 ? req.RegularPrice : await ComputeRegularPriceAsync(items))
            .Set(p => p.Items, items)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        var result = await _db.Packages.UpdateOneAsync(p => p.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Packages.DeleteOneAsync(p => p.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }

    // Keeps only items whose product actually exists, preserving slot key + quantity.
    private async Task<List<PackageItem>> BuildItemsAsync(List<SavePackageItemRequest> requested)
    {
        var productIds = requested.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var validIds = products.Select(p => p.Id).ToHashSet();

        return requested
            .Where(i => validIds.Contains(i.ProductId) && i.Quantity > 0)
            .Select(i => new PackageItem { SlotKey = i.SlotKey, ProductId = i.ProductId, Quantity = i.Quantity })
            .ToList();
    }

    private async Task<decimal> ComputeRegularPriceAsync(List<PackageItem> items)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        return items.Sum(i => productMap.TryGetValue(i.ProductId, out var p)
            ? (p.DiscountPrice ?? p.Price) * i.Quantity
            : 0m);
    }
}

// Shared mapping so public + admin endpoints return identically-shaped package DTOs.
internal static class PackageMapper
{
    public static async Task<List<PackageDto>> MapManyAsync(MongoDbContext db, List<Package> packages)
    {
        var productIds = packages.SelectMany(p => p.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        return packages.Select(pkg => new PackageDto(
            pkg.Id,
            pkg.Name,
            pkg.Description,
            pkg.ImageUrl,
            pkg.IsActive,
            pkg.IsFeatured,
            pkg.RegularPrice,
            pkg.PackagePrice,
            pkg.Items.Select(i =>
            {
                var product = productMap.GetValueOrDefault(i.ProductId);
                return new PackageItemDto(
                    i.SlotKey,
                    i.ProductId,
                    product?.Name ?? "Unknown Product",
                    product?.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl ?? product?.Images.FirstOrDefault()?.ImageUrl,
                    product?.DiscountPrice ?? product?.Price ?? 0,
                    i.Quantity,
                    product?.Stock ?? 0
                );
            }).ToList(),
            pkg.UpdatedAt
        )).ToList();
    }
}
