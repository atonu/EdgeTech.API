using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public ProductsController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
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
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var categories = await _db.Categories.Find(_ => true).ToListAsync();
        var brands = await _db.Brands.Find(_ => true).ToListAsync();

        var categorySlugToIds = categories
            .GroupBy(c => c.Slug)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Id).ToHashSet());

        if (!string.IsNullOrEmpty(category))
        {
            var matching = categories.Where(c => c.Slug == category).Select(c => c.Id).ToHashSet();
            var parentIds = categories.Where(c => c.Slug == category).Select(c => c.Id).ToHashSet();
            foreach (var sub in categories.Where(c => c.ParentCategoryId.HasValue && parentIds.Contains(c.ParentCategoryId.Value)))
                matching.Add(sub.Id);
            categorySlugToIds[category] = matching;
        }

        var brandIds = string.IsNullOrEmpty(brand)
            ? null
            : brands.Where(b => b.Slug == brand).Select(b => b.Id).ToHashSet();

        var items = await _db.Products.Find(p => p.IsActive).ToListAsync();

        IEnumerable<Product> query = items;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLowerInvariant().Contains(term) || (p.Description ?? string.Empty).ToLowerInvariant().Contains(term));
        }

        if (!string.IsNullOrEmpty(category) && categorySlugToIds.TryGetValue(category, out var categoryIds))
            query = query.Where(p => categoryIds.Contains(p.CategoryId));

        if (brandIds != null && brandIds.Count > 0)
            query = query.Where(p => brandIds.Contains(p.BrandId));

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
            "name" or "name-asc" => query.OrderBy(p => p.Name),
            "popular" => query.OrderByDescending(p => p.Reviews.Count).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = query.Count();
        var pageItems = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var categoryMap = categories.ToDictionary(c => c.Id);
        var brandMap = brands.ToDictionary(b => b.Id);
        var dtos = pageItems.Select(p => MapToListDto(p, categoryMap, brandMap)).ToList();

        return Ok(new PagedResult<ProductListDto>(dtos, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize)));
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured([FromQuery] int count = 8)
    {
        count = Math.Clamp(count, 1, 50);
        var products = await _db.Products.Find(p => p.IsActive && p.IsFeatured)
            .SortByDescending(p => p.CreatedAt)
            .Limit(count)
            .ToListAsync();

        var categoryMap = (await _db.Categories.Find(_ => true).ToListAsync()).ToDictionary(c => c.Id);
        var brandMap = (await _db.Brands.Find(_ => true).ToListAsync()).ToDictionary(b => b.Id);

        return Ok(products.Select(p => MapToListDto(p, categoryMap, brandMap)));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await _db.Products.Find(p => p.Slug == slug && p.IsActive).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        var category = await _db.Categories.Find(c => c.Id == product.CategoryId).FirstOrDefaultAsync();
        var brand = await _db.Brands.Find(b => b.Id == product.BrandId).FirstOrDefaultAsync();

        return Ok(MapToDto(product, category, brand));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var categoryExists = await _db.Categories.Find(c => c.Id == req.CategoryId && c.IsActive).AnyAsync();
        if (!categoryExists) return BadRequest(new { message = "A valid category is required for every product" });

        var brandExists = await _db.Brands.Find(b => b.Id == req.BrandId && b.IsActive).AnyAsync();
        if (!brandExists) return BadRequest(new { message = "A valid brand is required for every product" });

        var slug = GenerateSlug(req.Name);
        if (await _db.Products.Find(p => p.Slug == slug).AnyAsync())
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var nextProductId = await _ids.NextAsync("products");

        var specs = new List<ProductSpecification>();
        if (req.Specifications != null)
        {
            foreach (var spec in req.Specifications.OrderBy(s => s.DisplayOrder))
            {
                specs.Add(new ProductSpecification
                {
                    Id = await _ids.NextAsync("productSpecifications"),
                    ProductId = nextProductId,
                    Key = spec.Key,
                    Value = spec.Value,
                    DisplayOrder = spec.DisplayOrder
                });
            }
        }

        var product = new Product
        {
            Id = nextProductId,
            Name = req.Name,
            Slug = slug,
            Description = req.Description,
            ShortDescription = req.ShortDescription,
            Price = req.Price,
            DiscountPrice = req.DiscountPrice,
            SKU = req.SKU,
            Stock = req.Stock,
            CategoryId = req.CategoryId,
            BrandId = req.BrandId,
            IsFeatured = req.IsFeatured,
            Specifications = specs
        };

        await _db.Products.InsertOneAsync(product);
        return CreatedAtAction(nameof(GetBySlug), new { slug = product.Slug }, new { id = product.Id, slug = product.Slug });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest req)
    {
        var product = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        var categoryExists = await _db.Categories.Find(c => c.Id == req.CategoryId && c.IsActive).AnyAsync();
        if (!categoryExists) return BadRequest(new { message = "A valid category is required for every product" });

        var brandExists = await _db.Brands.Find(b => b.Id == req.BrandId && b.IsActive).AnyAsync();
        if (!brandExists) return BadRequest(new { message = "A valid brand is required for every product" });

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
            var specs = new List<ProductSpecification>();
            foreach (var spec in req.Specifications.OrderBy(s => s.DisplayOrder))
            {
                specs.Add(new ProductSpecification
                {
                    Id = await _ids.NextAsync("productSpecifications"),
                    ProductId = product.Id,
                    Key = spec.Key,
                    Value = spec.Value,
                    DisplayOrder = spec.DisplayOrder
                });
            }
            product.Specifications = specs;
        }

        await _db.Products.ReplaceOneAsync(p => p.Id == id, product);
        return NoContent();
    }

    [HttpPatch("{id}/featured")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleFeatured(int id, [FromBody] ToggleFeaturedRequest req)
    {
        var update = Builders<Product>.Update
            .Set(p => p.IsFeatured, req.IsFeatured)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);
        var filter = Builders<Product>.Filter.Eq(p => p.Id, id);

        var result = await _db.Products.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Product, Product>
        {
            ReturnDocument = ReturnDocument.After
        });

        if (result == null) return NotFound();
        return Ok(new { isFeatured = result.IsFeatured });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Products.UpdateOneAsync(p => p.Id == id,
            Builders<Product>.Update.Set(p => p.IsActive, false).Set(p => p.UpdatedAt, DateTime.UtcNow));
        if (result.MatchedCount == 0) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddImage(int id, [FromBody] AddProductImageRequest req)
    {
        // Only accept paths the frontend's own uploader produces — never an arbitrary external URL.
        if (string.IsNullOrWhiteSpace(req.ImageUrl) || !req.ImageUrl.StartsWith("/product-images/"))
            return BadRequest(new { message = "imageUrl must be a path under /product-images/." });

        var product = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        var image = new ProductImage
        {
            Id = await _ids.NextAsync("productImages"),
            ProductId = id,
            ImageUrl = req.ImageUrl,
            IsPrimary = !product.Images.Any(),
            DisplayOrder = product.Images.Count
        };

        product.Images.Add(image);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.Products.ReplaceOneAsync(p => p.Id == id, product);
        return Ok(new ProductImageDto(image.Id, image.ImageUrl, image.IsPrimary, image.DisplayOrder));
    }

    [HttpPatch("{id}/images/{imageId}/primary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetPrimaryImage(int id, int imageId)
    {
        var product = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        if (!product.Images.Any(i => i.Id == imageId)) return NotFound();

        foreach (var img in product.Images)
            img.IsPrimary = img.Id == imageId;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.Products.ReplaceOneAsync(p => p.Id == id, product);
        return NoContent();
    }

    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var product = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return NotFound();

        var wasPrimary = image.IsPrimary;
        product.Images = product.Images.Where(i => i.Id != imageId).ToList();
        if (wasPrimary)
        {
            var newPrimary = product.Images.FirstOrDefault();
            if (newPrimary != null) newPrimary.IsPrimary = true;
        }
        product.UpdatedAt = DateTime.UtcNow;

        await _db.Products.ReplaceOneAsync(p => p.Id == id, product);
        return NoContent();
    }

    private static ProductListDto MapToListDto(Product p, Dictionary<int, Category> categoryMap, Dictionary<int, Brand> brandMap) => new(
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

    private static ProductDto MapToDto(Product p, Category? category, Brand? brand) => new(
        p.Id, p.Name, p.Slug, p.Description, p.ShortDescription,
        p.Price, p.DiscountPrice, p.SKU, p.Stock,
        p.IsFeatured, p.IsActive,
        p.CategoryId, category?.Name ?? "Unknown", category?.Slug ?? string.Empty,
        p.BrandId, brand?.Name ?? "Unknown", brand?.Slug ?? string.Empty,
        p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
        p.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.IsPrimary, i.DisplayOrder)).ToList(),
        p.Specifications.OrderBy(s => s.DisplayOrder).Select(s => new ProductSpecDto(s.Id, s.Key, s.Value, s.DisplayOrder)).ToList(),
        p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
        p.Reviews.Count,
        p.CreatedAt
    );

    private static string GenerateSlug(string name) =>
        name.ToLower().Replace(" ", "-").Replace("/", "-").Replace("(", "").Replace(")", "")
            .Replace(",", "").Replace(".", "").Trim('-');
}
