using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EdgeTech.API.Models;

namespace EdgeTech.API.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<RecentlyViewed> RecentlyViewed => Set<RecentlyViewed>();
    public DbSet<PackageBuild> PackageBuilds => Set<PackageBuild>();
    public DbSet<PackageComponent> PackageComponents => Set<PackageComponent>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Category self-reference
        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Product
        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Product>()
            .Property(p => p.DiscountPrice)
            .HasColumnType("decimal(18,2)");

        // Order
        builder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        // OrderItem
        builder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(18,2)");

        // PackageBuild
        builder.Entity<PackageBuild>()
            .Property(pb => pb.TotalPrice)
            .HasColumnType("decimal(18,2)");

        // Indexes
        builder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        builder.Entity<Brand>().HasIndex(b => b.Slug).IsUnique();

        builder.Entity<RecentlyViewed>()
            .HasIndex(rv => new { rv.UserId, rv.ProductId }).IsUnique();
    }
}
