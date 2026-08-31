using EdgeTech.API.Models;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EdgeTech.API.Data;

public class MongoDbContext
{
    static MongoDbContext()
    {
        var pack = new ConventionPack
        {
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("GlobalConventions", pack, t => true);
    }

    public IMongoDatabase Database { get; }

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var databaseName = configuration["MongoDb:DatabaseName"] ?? "EdgeTechDB";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("MongoDb:ConnectionString is not configured.");

        var client = new MongoClient(connectionString);
        Database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<ApplicationUser> Users => Database.GetCollection<ApplicationUser>("users");
    public IMongoCollection<Category> Categories => Database.GetCollection<Category>("categories");
    public IMongoCollection<Brand> Brands => Database.GetCollection<Brand>("brands");
    public IMongoCollection<Product> Products => Database.GetCollection<Product>("products");
    public IMongoCollection<ServiceItem> Services => Database.GetCollection<ServiceItem>("services");
    public IMongoCollection<ProductGroup> ProductGroups => Database.GetCollection<ProductGroup>("productGroups");
    public IMongoCollection<CartItem> CartItems => Database.GetCollection<CartItem>("cartItems");
    public IMongoCollection<Order> Orders => Database.GetCollection<Order>("orders");
    public IMongoCollection<PackageBuild> PackageBuilds => Database.GetCollection<PackageBuild>("packageBuilds");
    public IMongoCollection<RecentlyViewed> RecentlyViewed => Database.GetCollection<RecentlyViewed>("recentlyViewed");
    public IMongoCollection<Counter> Counters => Database.GetCollection<Counter>("counters");
}
