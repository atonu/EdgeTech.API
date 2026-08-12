using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public CategoriesController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories.Find(c => c.IsActive).SortBy(c => c.DisplayOrder).ToListAsync();
        var roots = categories.Where(c => c.ParentCategoryId == null).ToList();
        return Ok(roots.Select(c => MapToDto(c, categories)).ToList());
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var categories = await _db.Categories.Find(_ => true).ToListAsync();
        var category = categories.FirstOrDefault(c => c.Slug == slug);
        if (category == null) return NotFound();
        return Ok(MapToDto(category, categories));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        var slug = req.Name.ToLower().Replace(" ", "-").Replace("&", "and");
        if (await _db.Categories.Find(c => c.Slug == slug).AnyAsync())
            slug = $"{slug}-{Guid.NewGuid().ToString()[..4]}";

        var cat = new Category
        {
            Id = await _ids.NextAsync("categories"),
            Name = req.Name,
            Slug = slug,
            Description = req.Description,
            ImageUrl = req.ImageUrl,
            DisplayOrder = req.DisplayOrder,
            ParentCategoryId = req.ParentCategoryId,
            IsActive = true
        };

        await _db.Categories.InsertOneAsync(cat);
        return CreatedAtAction(nameof(GetBySlug), new { slug = cat.Slug }, MapToDto(cat, []));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest req)
    {
        var update = Builders<Category>.Update
            .Set(c => c.Name, req.Name)
            .Set(c => c.Description, req.Description)
            .Set(c => c.ImageUrl, req.ImageUrl)
            .Set(c => c.DisplayOrder, req.DisplayOrder)
            .Set(c => c.IsActive, req.IsActive)
            .Set(c => c.ParentCategoryId, req.ParentCategoryId);

        var result = await _db.Categories.UpdateOneAsync(c => c.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Categories.UpdateOneAsync(c => c.Id == id, Builders<Category>.Update.Set(c => c.IsActive, false));
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    private static CategoryDto MapToDto(Category c, List<Category> all) => new(
        c.Id,
        c.Name,
        c.Slug,
        c.Description,
        c.ImageUrl,
        c.DisplayOrder,
        c.IsActive,
        c.ParentCategoryId,
        all.Where(sub => sub.ParentCategoryId == c.Id && sub.IsActive)
            .OrderBy(sub => sub.DisplayOrder)
            .Select(sub => MapToDto(sub, all))
            .ToList()
    );
}

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public BrandsController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brands = await _db.Brands.Find(b => b.IsActive).SortBy(b => b.Name).ToListAsync();
        return Ok(brands.Select(b => new BrandDto(b.Id, b.Name, b.Slug, b.LogoUrl, b.Description, b.IsActive)));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var brand = await _db.Brands.Find(b => b.Slug == slug).FirstOrDefaultAsync();
        if (brand == null) return NotFound();
        return Ok(new BrandDto(brand.Id, brand.Name, brand.Slug, brand.LogoUrl, brand.Description, brand.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest req)
    {
        var slug = req.Name.ToLower().Replace(" ", "-");
        if (await _db.Brands.Find(b => b.Slug == slug).AnyAsync())
            slug = $"{slug}-{Guid.NewGuid().ToString()[..4]}";

        var brand = new Brand
        {
            Id = await _ids.NextAsync("brands"),
            Name = req.Name,
            Slug = slug,
            Description = req.Description,
            LogoUrl = req.LogoUrl,
            IsActive = true
        };

        await _db.Brands.InsertOneAsync(brand);
        return CreatedAtAction(nameof(GetBySlug), new { slug = brand.Slug }, new BrandDto(brand.Id, brand.Name, brand.Slug, brand.LogoUrl, brand.Description, brand.IsActive));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBrandRequest req)
    {
        var update = Builders<Brand>.Update
            .Set(b => b.Name, req.Name)
            .Set(b => b.Description, req.Description)
            .Set(b => b.LogoUrl, req.LogoUrl)
            .Set(b => b.IsActive, req.IsActive);

        var result = await _db.Brands.UpdateOneAsync(b => b.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Brands.UpdateOneAsync(b => b.Id == id, Builders<Brand>.Update.Set(b => b.IsActive, false));
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }
}
