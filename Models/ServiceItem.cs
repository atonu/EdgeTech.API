namespace EdgeTech.API.Models;

public class ServiceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public decimal Price { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public bool IsPerUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
