namespace EdgeTech.API.Models;

public class ProductGroup
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty; // best-sellers, most-popular, new-arrivals
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<int> ProductIds { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
