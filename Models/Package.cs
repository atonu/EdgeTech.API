using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

// Admin-authored bundle template. Selecting a package on the storefront drops all of its
// component products into the cart as a single line billed at PackagePrice (not the sum of items).
[BsonIgnoreExtraElements]
public class Package
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // When true the package is surfaced in the storefront "Best Selling" row alongside products.
    public bool IsFeatured { get; set; }

    // RegularPrice is a snapshot of the components' individual prices at save time (sum of discount ?? price).
    // PackagePrice is the admin's discounted bundle price shown to the customer.
    public decimal RegularPrice { get; set; }
    public decimal PackagePrice { get; set; }

    public List<PackageItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PackageItem
{
    public string SlotKey { get; set; } = string.Empty; // camera_1, dvr, monitor, etc. (mirrors the solution builder slots)
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}
