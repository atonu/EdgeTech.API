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
        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("MongoDb__ConnectionString")
            ?? "mongodb://localhost:27017";

        if (connectionString.Contains("<user>", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("SET_IN_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("cluster", StringComparison.OrdinalIgnoreCase) && connectionString.Contains("mongodb+srv", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = "mongodb://localhost:27017";
        }

        var databaseName = configuration["MongoDb:DatabaseName"] ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? "edgetechdb"; //db name changed

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
    public IMongoCollection<Package> Packages => Database.GetCollection<Package>("packages");
    public IMongoCollection<RecentlyViewed> RecentlyViewed => Database.GetCollection<RecentlyViewed>("recentlyViewed");
    public IMongoCollection<Counter> Counters => Database.GetCollection<Counter>("counters");
    public IMongoCollection<PolicyPage> PolicyPages => Database.GetCollection<PolicyPage>("policyPages");
    public IMongoCollection<Feedback> Feedbacks => Database.GetCollection<Feedback>("feedbacks");
    public IMongoCollection<HeroCarousel> HeroCarousels => Database.GetCollection<HeroCarousel>("heroCarousel");
    public IMongoCollection<HappyCustomer> HappyCustomers => Database.GetCollection<HappyCustomer>("happyCustomers");
}
