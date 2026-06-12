namespace EdgeTech.API.Models;

public class RecentlyViewed
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
