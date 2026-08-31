using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

[BsonIgnoreExtraElements]
public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Product> Products { get; set; } = [];
}
