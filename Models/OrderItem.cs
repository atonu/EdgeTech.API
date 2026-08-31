using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

[BsonIgnoreExtraElements]
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ProductSnapshot { get; set; } = string.Empty; // JSON snapshot

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
