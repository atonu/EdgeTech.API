using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;

    public CategoriesController(AppDbContext db, IBlobStorageService blob) { _db = db; _blob = blob; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return Ok(categories.Select(MapToDto));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var category = await _db.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Slug == slug);
        if (category == null) return NotFound();
        return Ok(MapToDto(category));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        var slug = req.Name.ToLower().Replace(" ", "-").Replace("&", "and");
        if (await _db.Categories.AnyAsync(c => c.Slug == slug))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..4]}";

        var cat = new Category
        {
            Name = req.Name, Slug = slug,
            Description = req.Description, ImageUrl = req.ImageUrl,
            DisplayOrder = req.DisplayOrder, ParentCategoryId = req.ParentCategoryId
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = cat.Slug }, MapToDto(cat));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest req)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.Name = req.Name; cat.Description = req.Description;
        cat.ImageUrl = req.ImageUrl; cat.DisplayOrder = req.DisplayOrder;
        cat.IsActive = req.IsActive; cat.ParentCategoryId = req.ParentCategoryId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryDto MapToDto(Category c) => new(
        c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.DisplayOrder, c.IsActive,
        c.ParentCategoryId, c.SubCategories?.Select(sub => MapToDto(sub)).ToList()
    );
}

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;

    public BrandsController(AppDbContext db, IBlobStorageService blob) { _db = db; _blob = blob; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brands = await _db.Brands.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return Ok(brands.Select(b => new BrandDto(b.Id, b.Name, b.Slug, b.LogoUrl, b.Description, b.IsActive)));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Slug == slug);
        if (brand == null) return NotFound();
        return Ok(new BrandDto(brand.Id, brand.Name, brand.Slug, brand.LogoUrl, brand.Description, brand.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest req)
    {
        var slug = req.Name.ToLower().Replace(" ", "-");
        var brand = new Brand { Name = req.Name, Slug = slug, Description = req.Description, LogoUrl = req.LogoUrl };
        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = brand.Slug }, new BrandDto(brand.Id, brand.Name, brand.Slug, brand.LogoUrl, brand.Description, brand.IsActive));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBrandRequest req)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand == null) return NotFound();
        brand.Name = req.Name; brand.Description = req.Description;
        brand.LogoUrl = req.LogoUrl; brand.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand == null) return NotFound();
        brand.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
