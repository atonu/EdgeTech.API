using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;

    public ProductsController(AppDbContext db, IBlobStorageService blob)
    {
        _db = db;
        _blob = blob;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? brand,
        [FromQuery] bool? featured,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category.Slug == category || (p.Category.ParentCategory != null && p.Category.ParentCategory.Slug == category));

        if (!string.IsNullOrEmpty(brand))
            query = query.Where(p => p.Brand.Slug == brand);

        if (featured.HasValue)
            query = query.Where(p => p.IsFeatured == featured.Value);

        if (minPrice.HasValue)
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price-desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = items.Select(p => MapToListDto(p)).ToList();
        return Ok(new PagedResult<ProductListDto>(dtos, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize)));
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured([FromQuery] int count = 8)
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();

        return Ok(products.Select(MapToListDto));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Category).ThenInclude(c => c.ParentCategory)
            .Include(p => p.Brand)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Specifications.OrderBy(s => s.DisplayOrder))
            .Include(p => p.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product == null) return NotFound();

        return Ok(MapToDto(product));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var slug = GenerateSlug(req.Name);
        if (await _db.Products.AnyAsync(p => p.Slug == slug))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var product = new Product
        {
            Name = req.Name, Slug = slug,
            Description = req.Description, ShortDescription = req.ShortDescription,
            Price = req.Price, DiscountPrice = req.DiscountPrice,
            SKU = req.SKU, Stock = req.Stock,
            CategoryId = req.CategoryId, BrandId = req.BrandId,
            IsFeatured = req.IsFeatured
        };

        if (req.Specifications != null)
            for (int i = 0; i < req.Specifications.Count; i++)
                product.Specifications.Add(new ProductSpecification { Key = req.Specifications[i].Key, Value = req.Specifications[i].Value, DisplayOrder = i });

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = product.Slug }, new { id = product.Id, slug = product.Slug });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest req)
    {
        var product = await _db.Products.Include(p => p.Specifications).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        product.Name = req.Name;
        product.Description = req.Description;
        product.ShortDescription = req.ShortDescription;
        product.Price = req.Price;
        product.DiscountPrice = req.DiscountPrice;
        product.SKU = req.SKU;
        product.Stock = req.Stock;
        product.CategoryId = req.CategoryId;
        product.BrandId = req.BrandId;
        product.IsFeatured = req.IsFeatured;
        product.IsActive = req.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        if (req.Specifications != null)
        {
            _db.ProductSpecifications.RemoveRange(product.Specifications);
            for (int i = 0; i < req.Specifications.Count; i++)
                product.Specifications.Add(new ProductSpecification { Key = req.Specifications[i].Key, Value = req.Specifications[i].Value, DisplayOrder = i });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/featured")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleFeatured(int id, [FromBody] ToggleFeaturedRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsFeatured = req.IsFeatured;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { isFeatured = product.IsFeatured });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        var url = await _blob.UploadAsync(file, "edgetech-products", $"products/{id}");
        var image = new ProductImage
        {
            ProductId = id,
            ImageUrl = url,
            IsPrimary = !product.Images.Any(),
            DisplayOrder = product.Images.Count
        };
        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync();
        return Ok(new ProductImageDto(image.Id, image.ImageUrl, image.IsPrimary, image.DisplayOrder));
    }

    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var image = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (image == null) return NotFound();
        await _blob.DeleteAsync(image.ImageUrl);
        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Helpers
    private static ProductListDto MapToListDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Price, p.DiscountPrice,
        p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
        p.Stock, p.IsFeatured, p.Category.Name, p.Brand.Name
    );

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description, p.ShortDescription,
        p.Price, p.DiscountPrice, p.SKU, p.Stock,
        p.IsFeatured, p.IsActive,
        p.CategoryId, p.Category.Name, p.Category.Slug,
        p.BrandId, p.Brand.Name, p.Brand.Slug,
        p.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.IsPrimary, i.DisplayOrder)).ToList(),
        p.Specifications.Select(s => new ProductSpecDto(s.Id, s.Key, s.Value, s.DisplayOrder)).ToList(),
        p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
        p.Reviews.Count,
        p.CreatedAt
    );

    private static string GenerateSlug(string name) =>
        name.ToLower().Replace(" ", "-").Replace("/", "-").Replace("(", "").Replace(")", "")
            .Replace(",", "").Replace(".", "").Trim('-');
}
