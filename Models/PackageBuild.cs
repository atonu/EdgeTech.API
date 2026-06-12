namespace EdgeTech.API.Models;

public class PackageBuild
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = "My CCTV Package";
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<PackageComponent> Components { get; set; } = [];
}

public class PackageComponent
{
    public int Id { get; set; }
    public int PackageBuildId { get; set; }
    public string SlotKey { get; set; } = string.Empty; // camera_1, dvr, monitor, etc.
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;

    public PackageBuild PackageBuild { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
