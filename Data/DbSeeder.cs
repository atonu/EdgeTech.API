using EdgeTech.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EdgeTech.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();

        // Seed Roles
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed Admin user
        const string adminEmail = "admin@edgetech.com.bd";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "EdgeTech",
                LastName = "Admin",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123456");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed Brands
        if (!await db.Brands.AnyAsync())
        {
            var brands = new[]
            {
                new Brand { Name = "Hikvision", Slug = "hikvision", Description = "World's leading video surveillance manufacturer" },
                new Brand { Name = "Dahua", Slug = "dahua", Description = "Leading solution provider in the global video-centric AIoT industry" },
                new Brand { Name = "Reolink", Slug = "reolink", Description = "Smart home security made easy" },
                new Brand { Name = "Imou", Slug = "imou", Description = "Smart living security brand by Dahua" },
                new Brand { Name = "TP-Link", Slug = "tp-link", Description = "World's #1 provider of consumer WiFi" },
                new Brand { Name = "Uniview", Slug = "uniview", Description = "IP video surveillance innovator" },
                new Brand { Name = "CP Plus", Slug = "cp-plus", Description = "Leading surveillance brand" },
                new Brand { Name = "Samsung", Slug = "samsung", Description = "Electronics giant" },
                new Brand { Name = "Seagate", Slug = "seagate", Description = "Data storage solutions" },
                new Brand { Name = "Western Digital", Slug = "western-digital", Description = "Data storage leader" },
            };
            await db.Brands.AddRangeAsync(brands);
            await db.SaveChangesAsync();
        }

        // Seed Categories
        if (!await db.Categories.AnyAsync())
        {
            // Parent categories
            var cctvParent = new Category { Name = "CCTV & Surveillance", Slug = "cctv-surveillance", DisplayOrder = 1 };
            var networkingParent = new Category { Name = "Networking", Slug = "networking", DisplayOrder = 2 };
            var storageParent = new Category { Name = "Storage", Slug = "storage", DisplayOrder = 3 };
            var accessoriesParent = new Category { Name = "Accessories", Slug = "accessories", DisplayOrder = 4 };

            await db.Categories.AddRangeAsync(cctvParent, networkingParent, storageParent, accessoriesParent);
            await db.SaveChangesAsync();

            // Sub-categories
            var subCategories = new[]
            {
                new Category { Name = "IP Cameras", Slug = "ip-cameras", ParentCategoryId = cctvParent.Id, DisplayOrder = 1 },
                new Category { Name = "Analog Cameras", Slug = "analog-cameras", ParentCategoryId = cctvParent.Id, DisplayOrder = 2 },
                new Category { Name = "PTZ Cameras", Slug = "ptz-cameras", ParentCategoryId = cctvParent.Id, DisplayOrder = 3 },
                new Category { Name = "Dome Cameras", Slug = "dome-cameras", ParentCategoryId = cctvParent.Id, DisplayOrder = 4 },
                new Category { Name = "Bullet Cameras", Slug = "bullet-cameras", ParentCategoryId = cctvParent.Id, DisplayOrder = 5 },
                new Category { Name = "DVR / NVR", Slug = "dvr-nvr", ParentCategoryId = cctvParent.Id, DisplayOrder = 6 },
                new Category { Name = "Monitor", Slug = "monitor", ParentCategoryId = cctvParent.Id, DisplayOrder = 7 },
                new Category { Name = "Camera Accessories", Slug = "camera-accessories", ParentCategoryId = cctvParent.Id, DisplayOrder = 8 },
                new Category { Name = "Network Switch", Slug = "network-switch", ParentCategoryId = networkingParent.Id, DisplayOrder = 1 },
                new Category { Name = "Router", Slug = "router", ParentCategoryId = networkingParent.Id, DisplayOrder = 2 },
                new Category { Name = "HDD", Slug = "hdd", ParentCategoryId = storageParent.Id, DisplayOrder = 1 },
                new Category { Name = "SSD", Slug = "ssd", ParentCategoryId = storageParent.Id, DisplayOrder = 2 },
                new Category { Name = "UPS", Slug = "ups", ParentCategoryId = accessoriesParent.Id, DisplayOrder = 1 },
                new Category { Name = "Power Adapter", Slug = "power-adapter", ParentCategoryId = accessoriesParent.Id, DisplayOrder = 2 },
                new Category { Name = "Cable", Slug = "cable", ParentCategoryId = accessoriesParent.Id, DisplayOrder = 3 },
            };
            await db.Categories.AddRangeAsync(subCategories);
            await db.SaveChangesAsync();
        }

        // Seed sample products
        if (!await db.Products.AnyAsync())
        {
            var hikvision = await db.Brands.FirstAsync(b => b.Slug == "hikvision");
            var dahua = await db.Brands.FirstAsync(b => b.Slug == "dahua");
            var ipCam = await db.Categories.FirstAsync(c => c.Slug == "ip-cameras");
            var analogCam = await db.Categories.FirstAsync(c => c.Slug == "analog-cameras");
            var dvrNvr = await db.Categories.FirstAsync(c => c.Slug == "dvr-nvr");
            var hdd = await db.Categories.FirstAsync(c => c.Slug == "hdd");
            var accessories = await db.Categories.FirstAsync(c => c.Slug == "camera-accessories");

            var products = new[]
            {
                new Product
                {
                    Name = "Hikvision DS-2CD2147G2-LU 4MP ColorVu",
                    Slug = "hikvision-ds-2cd2147g2-lu-4mp",
                    ShortDescription = "4MP AcuSense Fixed Turret Network Camera with ColorVu technology",
                    Description = "The Hikvision DS-2CD2147G2-LU features ColorVu technology providing 24/7 full-color imaging. With 4MP resolution, AcuSense deep learning, and built-in mic, it's perfect for commercial surveillance.",
                    Price = 6500, DiscountPrice = 5800, SKU = "HIK-2CD2147G2-LU",
                    Stock = 25, IsFeatured = true, CategoryId = ipCam.Id, BrandId = hikvision.Id,
                    Specifications = new List<ProductSpecification>
                    {
                        new() { Key = "Resolution", Value = "4MP (2560×1440)" },
                        new() { Key = "Sensor", Value = "1/1.8\" Progressive Scan CMOS" },
                        new() { Key = "Lens", Value = "2.8mm / 4mm" },
                        new() { Key = "IR Range", Value = "60m ColorVu" },
                        new() { Key = "IP Rating", Value = "IP67" },
                        new() { Key = "Power", Value = "PoE (IEEE 802.3af)" },
                    }
                },
                new Product
                {
                    Name = "Dahua IPC-HDW2849H-S-IL 8MP Dual Light",
                    Slug = "dahua-ipc-hdw2849h-s-il-8mp",
                    ShortDescription = "8MP Smart Dual Light Fixed-focal Eyeball Network Camera",
                    Description = "Dahua 8MP camera with dual light (IR + warm light) for full-color night vision. Features AI features including SMD Plus technology for reduced false alarms.",
                    Price = 8500, DiscountPrice = 7500, SKU = "DAH-HDW2849H-IL",
                    Stock = 18, IsFeatured = true, CategoryId = ipCam.Id, BrandId = dahua.Id,
                    Specifications = new List<ProductSpecification>
                    {
                        new() { Key = "Resolution", Value = "8MP (3840×2160)" },
                        new() { Key = "Sensor", Value = "1/2.7\" CMOS" },
                        new() { Key = "Lens", Value = "2.8mm Fixed" },
                        new() { Key = "IR Range", Value = "30m IR + 30m Warm Light" },
                        new() { Key = "IP Rating", Value = "IP67" },
                        new() { Key = "Power", Value = "PoE (IEEE 802.3af)" },
                    }
                },
                new Product
                {
                    Name = "Hikvision DS-7208HQHI-K2 8Ch DVR",
                    Slug = "hikvision-ds-7208hqhi-k2-8ch-dvr",
                    ShortDescription = "8-Channel Turbo HD DVR supporting 5MP resolution",
                    Description = "Professional 8-channel DVR supporting multiple camera types: Turbo HD, AHD, HDCVI, CVBS. Supports up to 5MP resolution with H.265+ encoding.",
                    Price = 12500, DiscountPrice = null, SKU = "HIK-7208HQHI-K2",
                    Stock = 12, IsFeatured = true, CategoryId = dvrNvr.Id, BrandId = hikvision.Id,
                    Specifications = new List<ProductSpecification>
                    {
                        new() { Key = "Channels", Value = "8 Channel" },
                        new() { Key = "Max Resolution", Value = "5MP" },
                        new() { Key = "HDD Bays", Value = "2 x SATA" },
                        new() { Key = "Compression", Value = "H.265+/H.265" },
                        new() { Key = "HDMI Output", Value = "1080P" },
                    }
                },
                new Product
                {
                    Name = "Hikvision DS-2DE4A425IWG-E 4MP PTZ",
                    Slug = "hikvision-ds-2de4a425iwg-e-ptz",
                    ShortDescription = "4MP 25× Optical Zoom Network Speed Dome PTZ Camera",
                    Description = "Professional PTZ camera with 25× optical zoom, auto-tracking, and AcuSense deep learning. Ideal for large area surveillance.",
                    Price = 45000, DiscountPrice = 42000, SKU = "HIK-2DE4A425IWG",
                    Stock = 5, IsFeatured = false, CategoryId = ipCam.Id, BrandId = hikvision.Id,
                    Specifications = new List<ProductSpecification>
                    {
                        new() { Key = "Resolution", Value = "4MP" },
                        new() { Key = "Optical Zoom", Value = "25×" },
                        new() { Key = "Pan Range", Value = "360° continuous" },
                        new() { Key = "Tilt Range", Value = "-15° to 90°" },
                        new() { Key = "IR Range", Value = "100m" },
                    }
                },
                new Product
                {
                    Name = "Seagate SkyHawk 2TB Surveillance HDD",
                    Slug = "seagate-skyhawk-2tb-surveillance",
                    ShortDescription = "Optimized for 24/7 surveillance recording with up to 64 HD cameras",
                    Description = "Seagate SkyHawk is purpose-built for surveillance systems. Supports up to 64 cameras simultaneously with ImagePerfect firmware for zero dropped frames.",
                    Price = 6800, DiscountPrice = 6200, SKU = "SEA-SKYHAWK-2TB",
                    Stock = 30, IsFeatured = false, CategoryId = hdd.Id, BrandId = await db.Brands.Select(b => b.Id).FirstAsync(),
                    Specifications = new List<ProductSpecification>
                    {
                        new() { Key = "Capacity", Value = "2TB" },
                        new() { Key = "Interface", Value = "SATA 6Gb/s" },
                        new() { Key = "RPM", Value = "5400 RPM" },
                        new() { Key = "Cache", Value = "256MB" },
                        new() { Key = "Workload Rate", Value = "180 TB/year" },
                    }
                },
                new Product
                {
                    Name = "CCTV BNC Power Combo Cable 50m",
                    Slug = "cctv-bnc-power-combo-cable-50m",
                    ShortDescription = "All-in-one BNC video and power cable for analog CCTV cameras",
                    Description = "Ready-made combo cable with BNC video connector and DC power connector. 50 meters length, suitable for all analog CCTV cameras.",
                    Price = 850, DiscountPrice = null, SKU = "ACC-BNC-50M",
                    Stock = 100, IsFeatured = false, CategoryId = accessories.Id, BrandId = await db.Brands.Select(b => b.Id).FirstAsync(),
                    Specifications = new List<ProductSpecification>
                    {
                        new() { Key = "Length", Value = "50 Meters" },
                        new() { Key = "Connector", Value = "BNC + DC Power" },
                        new() { Key = "Core", Value = "0.5mm CCA" },
                    }
                },
            };

            foreach (var p in products)
            {
                db.Products.Add(p);
            }
            await db.SaveChangesAsync();
        }
    }
}
