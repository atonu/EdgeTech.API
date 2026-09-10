using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

public class Feedback
{
    [BsonId]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Category { get; set; } = "General"; // General, Support, ProductInquiry, OrderAssistance, BugReport

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int? Rating { get; set; } // 1 - 5 stars

    public string Status { get; set; } = "New"; // New, InProgress, Resolved, Archived

    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
