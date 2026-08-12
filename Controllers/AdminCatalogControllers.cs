using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public ServicesController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _db.Services.Find(s => s.IsActive)
            .SortBy(s => s.Name)
            .ToListAsync();

        return Ok(services.Select(s => new ServiceItemDto(s.Id, s.Name, s.Description, s.IsActive)));
    }
}

[ApiController]
[Route("api/admin/services")]
[Authorize(Roles = "Admin")]
public class AdminServicesController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public AdminServicesController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _db.Services.Find(_ => true).SortBy(s => s.Name).ToListAsync();
        return Ok(services.Select(s => new ServiceItemDto(s.Id, s.Name, s.Description, s.IsActive)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceItemRequest req)
    {
        var item = new ServiceItem
        {
            Id = await _ids.NextAsync("services"),
            Name = req.Name,
            Description = req.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Services.InsertOneAsync(item);
        return Ok(new ServiceItemDto(item.Id, item.Name, item.Description, item.IsActive));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceItemRequest req)
    {
        var update = Builders<ServiceItem>.Update
            .Set(s => s.Name, req.Name)
            .Set(s => s.Description, req.Description)
            .Set(s => s.IsActive, req.IsActive);

        var result = await _db.Services.UpdateOneAsync(s => s.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Services.DeleteOneAsync(s => s.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }
}

[ApiController]
[Route("api/product-groups")]
public class ProductGroupsController : ControllerBase
{
    private readonly MongoDbContext _db;

    public ProductGroupsController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetHomeGroups()
    {
        var groups = await _db.ProductGroups.Find(g => g.IsActive).ToListAsync();
        var products = await _db.Products.Find(p => p.IsActive).ToListAsync();
        var categories = await _db.Categories.Find(c => c.IsActive).ToListAsync();
        var brands = await _db.Brands.Find(b => b.IsActive).ToListAsync();

        var categoryMap = categories.ToDictionary(c => c.Id);
        var brandMap = brands.ToDictionary(b => b.Id);
        var productMap = products.ToDictionary(p => p.Id);

        List<ProductListDto> Build(string key) => groups
            .FirstOrDefault(g => g.Key == key)?.ProductIds
            .Where(productMap.ContainsKey)
            .Select(id =>
            {
                var p = productMap[id];
                return new ProductListDto(
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
                );
            })
            .ToList() ?? [];

        return Ok(new
        {
            bestSellers = Build("best-sellers"),
            mostPopular = Build("most-popular"),
            newArrivals = Build("new-arrivals")
        });
    }
}

[ApiController]
[Route("api/admin/product-groups")]
[Authorize(Roles = "Admin")]
public class AdminProductGroupsController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public AdminProductGroupsController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await _db.ProductGroups.Find(_ => true)
            .SortBy(g => g.Name)
            .ToListAsync();

        return Ok(groups.Select(g => new ProductGroupDto(g.Id, g.Key, g.Name, g.IsActive, g.ProductIds, g.UpdatedAt)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductGroupRequest req)
    {
        var group = new ProductGroup
        {
            Id = await _ids.NextAsync("productGroups"),
            Key = req.Key,
            Name = req.Name,
            IsActive = req.IsActive,
            ProductIds = req.ProductIds.Distinct().ToList(),
            UpdatedAt = DateTime.UtcNow
        };

        await _db.ProductGroups.InsertOneAsync(group);
        return Ok(new ProductGroupDto(group.Id, group.Key, group.Name, group.IsActive, group.ProductIds, group.UpdatedAt));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductGroupRequest req)
    {
        var update = Builders<ProductGroup>.Update
            .Set(g => g.Name, req.Name)
            .Set(g => g.IsActive, req.IsActive)
            .Set(g => g.ProductIds, req.ProductIds.Distinct().ToList())
            .Set(g => g.UpdatedAt, DateTime.UtcNow);

        var result = await _db.ProductGroups.UpdateOneAsync(g => g.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.ProductGroups.DeleteOneAsync(g => g.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return NoContent();
    }
}
