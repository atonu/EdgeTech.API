using EdgeTech.API.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace EdgeTech.API.Data;

public static class MongoDbInitializer
{
    public static async Task InitializeAsync(MongoDbContext db)
    {
        await CreateIndexesAsync(db);
        await SeedAsync(db);
    }

    private static async Task CreateIndexesAsync(MongoDbContext db)
    {
        await db.Users.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ApplicationUser>(Builders<ApplicationUser>.IndexKeys.Ascending(u => u.Email), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<ApplicationUser>(Builders<ApplicationUser>.IndexKeys.Ascending(u => u.Role))
        ]);

        await db.Categories.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Category>(Builders<Category>.IndexKeys.Ascending(c => c.Slug), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Category>(Builders<Category>.IndexKeys.Ascending(c => c.ParentCategoryId))
        ]);

        await db.Brands.Indexes.CreateOneAsync(
            new CreateIndexModel<Brand>(Builders<Brand>.IndexKeys.Ascending(b => b.Slug), new CreateIndexOptions { Unique = true }));

        await db.Products.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(p => p.Slug), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(p => p.CategoryId)),
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(p => p.BrandId)),
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(p => p.IsFeatured))
        ]);

        await db.CartItems.Indexes.CreateOneAsync(
            new CreateIndexModel<CartItem>(Builders<CartItem>.IndexKeys.Combine(
                Builders<CartItem>.IndexKeys.Ascending(c => c.UserId),
                Builders<CartItem>.IndexKeys.Ascending(c => c.ProductId)),
                new CreateIndexOptions { Unique = true }));

        await db.RecentlyViewed.Indexes.CreateOneAsync(
            new CreateIndexModel<RecentlyViewed>(Builders<RecentlyViewed>.IndexKeys.Combine(
                Builders<RecentlyViewed>.IndexKeys.Ascending(r => r.UserId),
                Builders<RecentlyViewed>.IndexKeys.Ascending(r => r.ProductId)),
                new CreateIndexOptions { Unique = true }));

        await db.Orders.Indexes.CreateOneAsync(new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.UserId)));
        await db.PackageBuilds.Indexes.CreateOneAsync(new CreateIndexModel<PackageBuild>(Builders<PackageBuild>.IndexKeys.Ascending(p => p.UserId)));
    }

    private static async Task SeedAsync(MongoDbContext db)
    {
        var hasher = new PasswordHasher<ApplicationUser>();

        var admin = await db.Users.Find(u => u.Email == "admin@edgetech.com.bd").FirstOrDefaultAsync();
        if (admin == null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = "admin@edgetech.com.bd",
                Email = "admin@edgetech.com.bd",
                FirstName = "EdgeTech",
                LastName = "Admin",
                Role = "Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, "Admin@123456");
            await db.Users.InsertOneAsync(user);
        }

        if (!await db.Brands.Find(_ => true).AnyAsync())
        {
            var brands = new[]
            {
                new Brand { Id = 1, Name = "Hikvision", Slug = "hikvision", Description = "World's leading video surveillance manufacturer" },
                new Brand { Id = 2, Name = "Dahua", Slug = "dahua", Description = "Leading solution provider in the global video-centric AIoT industry" },
                new Brand { Id = 3, Name = "Imou", Slug = "imou", Description = "Smart living security brand by Dahua" },
                new Brand { Id = 4, Name = "TP-Link", Slug = "tp-link", Description = "World's #1 provider of consumer WiFi" },
                new Brand { Id = 5, Name = "Uniview", Slug = "uniview", Description = "IP video surveillance innovator" },
                new Brand { Id = 6, Name = "Seagate", Slug = "seagate", Description = "Data storage solutions" },
                new Brand { Id = 7, Name = "Dell", Slug = "dell", Description = "Display and computing solutions" },
                new Brand { Id = 8, Name = "ZKTeco", Slug = "zkteco", Description = "Security and smart entrance solutions" },
                new Brand { Id = 9, Name = "Ruijie", Slug = "ruijie", Description = "Enterprise network infrastructure" },
            };

            await db.Brands.InsertManyAsync(brands);
        }

        if (!await db.Categories.Find(_ => true).AnyAsync())
        {
            var categories = new[]
            {
                new Category { Id = 1, Name = "CCTV & Surveillance", Slug = "cctv-surveillance", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Networking", Slug = "networking", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Storage", Slug = "storage", DisplayOrder = 3 },
                new Category { Id = 4, Name = "Accessories", Slug = "accessories", DisplayOrder = 4 },
                new Category { Id = 5, Name = "IP Cameras", Slug = "ip-cameras", ParentCategoryId = 1, DisplayOrder = 1 },
                new Category { Id = 6, Name = "Analog Cameras", Slug = "analog-cameras", ParentCategoryId = 1, DisplayOrder = 2 },
                new Category { Id = 7, Name = "DVR / NVR", Slug = "dvr-nvr", ParentCategoryId = 1, DisplayOrder = 3 },
                new Category { Id = 8, Name = "Monitor", Slug = "monitor", ParentCategoryId = 1, DisplayOrder = 4 },
                new Category { Id = 9, Name = "HDD", Slug = "hdd", ParentCategoryId = 3, DisplayOrder = 1 },
                new Category { Id = 10, Name = "Power Adapter", Slug = "power-adapter", ParentCategoryId = 4, DisplayOrder = 1 },
                new Category { Id = 11, Name = "Cable", Slug = "cable", ParentCategoryId = 4, DisplayOrder = 2 },
                new Category { Id = 12, Name = "UPS", Slug = "ups", ParentCategoryId = 4, DisplayOrder = 3 },
            };

            await db.Categories.InsertManyAsync(categories);
        }

        if (!await db.Products.Find(_ => true).AnyAsync())
        {
            var products = new[]
            {
                new Product
                {
                    Id = 1,
                    Name = "Hikvision DS-2CD2147G2-LU 4MP ColorVu",
                    Slug = "hikvision-ds-2cd2147g2-lu-4mp",
                    ShortDescription = "4MP AcuSense Fixed Turret Network Camera with ColorVu technology",
                    Description = "ColorVu camera with 24/7 full-color imaging and AcuSense.",
                    Price = 6500,
                    DiscountPrice = 5800,
                    SKU = "HIK-2CD2147G2-LU",
                    Stock = 25,
                    IsFeatured = true,
                    CategoryId = 5,
                    BrandId = 1,
                    Specifications =
                    [
                        new ProductSpecification { Id = 1, ProductId = 1, Key = "Resolution", Value = "4MP", DisplayOrder = 1 },
                        new ProductSpecification { Id = 2, ProductId = 1, Key = "IP Rating", Value = "IP67", DisplayOrder = 2 }
                    ]
                },
                new Product
                {
                    Id = 2,
                    Name = "Dahua IPC-HDW2849H-S-IL 8MP",
                    Slug = "dahua-ipc-hdw2849h-s-il-8mp",
                    ShortDescription = "8MP Smart Dual Light Fixed-focal Eyeball Network Camera",
                    Description = "8MP camera with dual light for full-color night vision.",
                    Price = 8500,
                    DiscountPrice = 7500,
                    SKU = "DAH-HDW2849H-IL",
                    Stock = 18,
                    IsFeatured = true,
                    CategoryId = 5,
                    BrandId = 2,
                    Specifications =
                    [
                        new ProductSpecification { Id = 3, ProductId = 2, Key = "Resolution", Value = "8MP", DisplayOrder = 1 },
                        new ProductSpecification { Id = 4, ProductId = 2, Key = "IR Range", Value = "30m", DisplayOrder = 2 }
                    ]
                },
                new Product
                {
                    Id = 3,
                    Name = "Hikvision DS-7208HQHI-K2 8Ch DVR",
                    Slug = "hikvision-ds-7208hqhi-k2-8ch-dvr",
                    ShortDescription = "8-Channel Turbo HD DVR supporting 5MP resolution",
                    Description = "Professional 8-channel DVR with H.265+.",
                    Price = 12500,
                    SKU = "HIK-7208HQHI-K2",
                    Stock = 12,
                    IsFeatured = true,
                    CategoryId = 7,
                    BrandId = 1,
                    Specifications =
                    [
                        new ProductSpecification { Id = 5, ProductId = 3, Key = "Channels", Value = "8", DisplayOrder = 1 },
                        new ProductSpecification { Id = 6, ProductId = 3, Key = "Compression", Value = "H.265+", DisplayOrder = 2 }
                    ]
                },
                new Product
                {
                    Id = 4,
                    Name = "Seagate SkyHawk 2TB Surveillance HDD",
                    Slug = "seagate-skyhawk-2tb-surveillance",
                    ShortDescription = "Surveillance-grade HDD",
                    Description = "Optimized for 24/7 surveillance workloads.",
                    Price = 6800,
                    DiscountPrice = 6200,
                    SKU = "SEA-SKYHAWK-2TB",
                    Stock = 30,
                    CategoryId = 9,
                    BrandId = 6,
                    Specifications =
                    [
                        new ProductSpecification { Id = 7, ProductId = 4, Key = "Capacity", Value = "2TB", DisplayOrder = 1 },
                        new ProductSpecification { Id = 8, ProductId = 4, Key = "Interface", Value = "SATA", DisplayOrder = 2 }
                    ]
                }
            };

            await db.Products.InsertManyAsync(products);
        }

        await SyncCountersAsync(db);
    }

    private static async Task SyncCountersAsync(MongoDbContext db)
    {
        var counters = new Dictionary<string, int>
        {
            ["brands"] = (await db.Brands.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["categories"] = (await db.Categories.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["products"] = (await db.Products.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["productImages"] = (await db.Products.Find(_ => true).ToListAsync()).SelectMany(p => p.Images).DefaultIfEmpty().Max(i => i?.Id ?? 0),
            ["productSpecifications"] = (await db.Products.Find(_ => true).ToListAsync()).SelectMany(p => p.Specifications).DefaultIfEmpty().Max(s => s?.Id ?? 0),
            ["cartItems"] = (await db.CartItems.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["orders"] = (await db.Orders.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["orderItems"] = (await db.Orders.Find(_ => true).ToListAsync()).SelectMany(o => o.Items).DefaultIfEmpty().Max(i => i?.Id ?? 0),
            ["packageBuilds"] = (await db.PackageBuilds.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["packageComponents"] = (await db.PackageBuilds.Find(_ => true).ToListAsync()).SelectMany(o => o.Components).DefaultIfEmpty().Max(i => i?.Id ?? 0),
            ["recentlyViewed"] = (await db.RecentlyViewed.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
        };

        foreach (var (name, value) in counters)
        {
            await db.Counters.ReplaceOneAsync(
                c => c.Id == name,
                new Counter { Id = name, Value = value },
                new ReplaceOptions { IsUpsert = true });
        }
    }
}
