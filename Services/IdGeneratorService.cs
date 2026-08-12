using EdgeTech.API.Data;
using EdgeTech.API.Models;
using MongoDB.Driver;

namespace EdgeTech.API.Services;

public interface IIdGeneratorService
{
    Task<int> NextAsync(string sequenceName);
}

public class IdGeneratorService : IIdGeneratorService
{
    private readonly MongoDbContext _db;

    public IdGeneratorService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<int> NextAsync(string sequenceName)
    {
        var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);
        var update = Builders<Counter>.Update.Inc(c => c.Value, 1);
        var options = new FindOneAndUpdateOptions<Counter, Counter>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var counter = await _db.Counters.FindOneAndUpdateAsync(filter, update, options);
        return counter.Value;
    }
}
