namespace EdgeTech.API.Models;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
