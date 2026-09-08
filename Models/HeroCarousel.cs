using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

/// <summary>
/// Singleton settings document driving the homepage hero carousel.
/// Exactly one document lives in the collection; slide count is admin-controlled.
/// </summary>
[BsonIgnoreExtraElements]
public class HeroCarousel
{
    /// <summary>Minimum slides the carousel is allowed to run with.</summary>
    public const int MinSlides = 2;

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public List<HeroSlide> Slides { get; set; } = new();

    /// <summary>Milliseconds each slide stays on screen before auto-advancing.</summary>
    public int AutoplayMs { get; set; } = 6000;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class HeroSlide
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ImageUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Cta { get; set; } = string.Empty;
    public string CtaLink { get; set; } = string.Empty;
    public int Order { get; set; }
}
