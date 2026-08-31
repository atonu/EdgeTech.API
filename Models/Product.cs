using System.Text.Json;
using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

[BsonIgnoreExtraElements]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? SKU { get; set; }
    public int Stock { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // FK
    public int CategoryId { get; set; }
    public int BrandId { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductSpecification> Specifications { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<WishlistItem> WishlistItems { get; set; } = [];
    public ICollection<RecentlyViewed> RecentlyViewedBy { get; set; } = [];
    public ICollection<PackageComponent> PackageComponents { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
