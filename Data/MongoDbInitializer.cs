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

        await db.Services.Indexes.CreateOneAsync(
            new CreateIndexModel<ServiceItem>(Builders<ServiceItem>.IndexKeys.Ascending(s => s.Name), new CreateIndexOptions { Unique = true }));

        await db.ProductGroups.Indexes.CreateOneAsync(
            new CreateIndexModel<ProductGroup>(Builders<ProductGroup>.IndexKeys.Ascending(g => g.Key), new CreateIndexOptions { Unique = true }));

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

        var admin = await db.Users.Find(u => u.Email == "admin@edgetech.com").FirstOrDefaultAsync();
        if (admin == null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = "admin@edgetech.com",
                Email = "admin@edgetech.com",
                FirstName = "EdgeTech",
                LastName = "Admin",
                Role = "Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, "admin9999");
            await db.Users.InsertOneAsync(user);
        }

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
            new Brand { Id = 10, Name = "APC", Slug = "apc", Description = "Power protection and backup solutions" },
            new Brand { Id = 11, Name = "Generic", Slug = "generic", Description = "Unbranded and generic accessories" },
        };
        await UpsertMissingAsync(db.Brands, brands, b => b.Id);

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
            new Category { Id = 13, Name = "Access Control", Slug = "access-control", DisplayOrder = 5 },
        };
        await UpsertMissingAsync(db.Categories, categories, c => c.Id);

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
                },
                new Product
                {
                    Id = 5, Name = "Hikvision 4MP Dome Camera", Slug = "hikvision-4mp-dome-camera",
                    ShortDescription = "4MP fixed dome network camera", Description = "Reliable 4MP dome camera for indoor and outdoor surveillance.",
                    Price = 12500, DiscountPrice = 10625, SKU = "HIK-DOME-4MP", Stock = 20, IsFeatured = true, CategoryId = 5, BrandId = 1,
                },
                new Product
                {
                    Id = 6, Name = "Dahua 8CH NVR System", Slug = "dahua-8ch-nvr-system",
                    ShortDescription = "8-channel network video recorder", Description = "8-channel NVR with support for up to 8MP cameras.",
                    Price = 35000, SKU = "DAH-NVR-8CH", Stock = 21, IsFeatured = true, CategoryId = 7, BrandId = 2,
                },
                new Product
                {
                    Id = 7, Name = "TP-Link 16-Port Switch", Slug = "tp-link-16-port-switch",
                    ShortDescription = "16-port unmanaged network switch", Description = "Gigabit unmanaged switch for expanding wired networks.",
                    Price = 8500, SKU = "TPL-SW-16P", Stock = 22, IsFeatured = true, CategoryId = 2, BrandId = 4,
                },
                new Product
                {
                    Id = 8, Name = "Seagate 4TB HDD", Slug = "seagate-4tb-hdd",
                    ShortDescription = "4TB surveillance-grade hard drive", Description = "High-endurance HDD built for continuous DVR/NVR recording.",
                    Price = 14000, SKU = "SEA-HDD-4TB", Stock = 23, IsFeatured = true, CategoryId = 9, BrandId = 6,
                },
                new Product
                {
                    Id = 9, Name = "Hikvision PTZ Camera", Slug = "hikvision-ptz-camera",
                    ShortDescription = "Pan-tilt-zoom network camera", Description = "Motorized PTZ camera with wide area coverage.",
                    Price = 45000, DiscountPrice = 38250, SKU = "HIK-PTZ-01", Stock = 24, IsFeatured = true, CategoryId = 5, BrandId = 1,
                },
                new Product
                {
                    Id = 10, Name = "Dahua 2MP Bullet Cam", Slug = "dahua-2mp-bullet-cam",
                    ShortDescription = "2MP analog bullet camera", Description = "Weatherproof bullet camera for outdoor CC surveillance.",
                    Price = 8900, SKU = "DAH-BUL-2MP", Stock = 0, IsFeatured = true, CategoryId = 6, BrandId = 2,
                },
                new Product
                {
                    Id = 11, Name = "APC UPS 1200VA", Slug = "apc-ups-1200va",
                    ShortDescription = "1200VA uninterruptible power supply", Description = "Backup power for DVR/NVR and networking equipment.",
                    Price = 11500, SKU = "APC-UPS-1200", Stock = 26, IsFeatured = true, CategoryId = 12, BrandId = 10,
                },
                new Product
                {
                    Id = 12, Name = "Cat6 Network Cable 305m", Slug = "cat6-network-cable-305m",
                    ShortDescription = "305m Cat6 UTP cable roll", Description = "Solid copper Cat6 cable for structured cabling runs.",
                    Price = 6500, SKU = "GEN-CAT6-305", Stock = 27, IsFeatured = true, CategoryId = 11, BrandId = 11,
                },
                new Product
                {
                    Id = 13, Name = "Hikvision 8MP Turret", Slug = "hikvision-8mp-turret",
                    ShortDescription = "8MP fixed turret network camera", Description = "High-resolution turret camera for detailed monitoring.",
                    Price = 28000, DiscountPrice = 23800, SKU = "HIK-TUR-8MP", Stock = 28, CategoryId = 5, BrandId = 1,
                },
                new Product
                {
                    Id = 14, Name = "Dell 24\" Monitor", Slug = "dell-24-inch-monitor",
                    ShortDescription = "24-inch Full HD monitor", Description = "Monitor for CCTV live view and workstation use.",
                    Price = 22000, SKU = "DELL-MON-24", Stock = 29, CategoryId = 8, BrandId = 7,
                },
                new Product
                {
                    Id = 15, Name = "Dahua XVR 16CH", Slug = "dahua-xvr-16ch",
                    ShortDescription = "16-channel HDCVI digital video recorder", Description = "Supports analog, IP, and HD-CVI cameras on one recorder.",
                    Price = 25000, SKU = "DAH-XVR-16", Stock = 30, CategoryId = 7, BrandId = 2,
                },
                new Product
                {
                    Id = 16, Name = "BNC Video Balun Pack", Slug = "bnc-video-balun-pack",
                    ShortDescription = "Video balun connector pack", Description = "Passive baluns for running analog video over UTP cable.",
                    Price = 2500, SKU = "GEN-BALUN-PK", Stock = 31, CategoryId = 4, BrandId = 11,
                },
                new Product
                {
                    Id = 17, Name = "Imou Cruiser 4MP", Slug = "imou-cruiser-4mp",
                    ShortDescription = "4MP Wi-Fi pan-tilt camera", Description = "Smart home Wi-Fi camera with motion tracking.",
                    Price = 15000, DiscountPrice = 12750, SKU = "IMO-CRU-4MP", Stock = 32, CategoryId = 5, BrandId = 3,
                },
                new Product
                {
                    Id = 18, Name = "Uniview 4CH NVR", Slug = "uniview-4ch-nvr",
                    ShortDescription = "4-channel network video recorder", Description = "Compact NVR for small-site surveillance setups.",
                    Price = 18000, SKU = "UNV-NVR-4CH", Stock = 33, CategoryId = 7, BrandId = 5,
                },
                new Product
                {
                    Id = 19, Name = "Ruijie 24-Port Switch", Slug = "ruijie-24-port-switch",
                    ShortDescription = "24-port enterprise network switch", Description = "Managed switch for enterprise network infrastructure.",
                    Price = 12000, SKU = "RJ-SW-24P", Stock = 34, CategoryId = 2, BrandId = 9,
                },
                new Product
                {
                    Id = 20, Name = "ZKTeco Biometric Access Control", Slug = "zkteco-biometric-access",
                    ShortDescription = "Fingerprint access control terminal", Description = "Biometric terminal for door access control and attendance.",
                    Price = 9500, SKU = "ZK-BIO-01", Stock = 35, CategoryId = 13, BrandId = 8,
                },
        };

        await UpsertMissingAsync(db.Products, products, p => p.Id);

        if (!await db.Services.Find(_ => true).AnyAsync())
        {
            await db.Services.InsertManyAsync(
            [
                new ServiceItem { Id = 1, Name = "Installation", Description = "Device installation and setup", IsActive = true },
                new ServiceItem { Id = 2, Name = "Repair", Description = "Hardware and wiring repair service", IsActive = true },
                new ServiceItem { Id = 3, Name = "Site Visitation", Description = "On-site survey and consultation", IsActive = true },
                new ServiceItem { Id = 4, Name = "Change Setup", Description = "Reconfigure existing setup", IsActive = true },
                new ServiceItem { Id = 5, Name = "Device Update", Description = "Firmware/software updates", IsActive = true },
            ]);
        }

        var groups = new[]
        {
            new ProductGroup { Id = 1, Key = "best-sellers", Name = "Best Sellers", IsActive = true, ProductIds = [5, 6, 7, 9], UpdatedAt = DateTime.UtcNow },
            new ProductGroup { Id = 2, Key = "most-popular", Name = "Most Popular", IsActive = true, ProductIds = [11, 12, 8, 13], UpdatedAt = DateTime.UtcNow },
            new ProductGroup { Id = 3, Key = "new-arrivals", Name = "New Arrivals", IsActive = true, ProductIds = [17, 18, 19, 20, 14, 15], UpdatedAt = DateTime.UtcNow },
        };
        foreach (var group in groups)
        {
            await db.ProductGroups.ReplaceOneAsync(g => g.Key == group.Key, group, new ReplaceOptions { IsUpsert = true });
        }

        await SyncCountersAsync(db);
    }

    private static async Task UpsertMissingAsync<T>(IMongoCollection<T> collection, IEnumerable<T> candidates, Func<T, int> idSelector)
    {
        var existingIds = (await collection.Find(_ => true).ToListAsync()).Select(idSelector).ToHashSet();
        var missing = candidates.Where(c => !existingIds.Contains(idSelector(c))).ToList();
        if (missing.Count > 0)
            await collection.InsertManyAsync(missing);
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
            ["services"] = (await db.Services.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
            ["productGroups"] = (await db.ProductGroups.Find(_ => true).SortByDescending(x => x.Id).FirstOrDefaultAsync())?.Id ?? 0,
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
