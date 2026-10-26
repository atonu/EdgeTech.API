using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

[BsonIgnoreExtraElements]
public class HappyCustomer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
