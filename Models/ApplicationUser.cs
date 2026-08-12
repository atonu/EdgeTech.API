namespace EdgeTech.API.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool EmailConfirmed { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<WishlistItem> WishlistItems { get; set; } = [];
    public ICollection<RecentlyViewed> RecentlyViewed { get; set; } = [];
    public ICollection<PackageBuild> PackageBuilds { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
