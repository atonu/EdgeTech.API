using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

[BsonIgnoreExtraElements]
public class PolicyPage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
    public List<PolicySection> Sections { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class PolicySection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HighlightTitle { get; set; }
    public string? HighlightText { get; set; }
    public List<PolicySubItem>? SubItems { get; set; }
    public List<string>? ListItems { get; set; }
    public int Order { get; set; }
}

[BsonIgnoreExtraElements]
public class PolicySubItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Tag { get; set; }
}
