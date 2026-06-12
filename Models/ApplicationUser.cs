using Microsoft.AspNetCore.Identity;

namespace EdgeTech.API.Models;

public class ApplicationUser : IdentityUser
{
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
