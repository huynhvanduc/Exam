using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> options, ILogger<MongoDbContext> logger)
    {
        MongoClassMaps.Register();

        var settings = options.Value;
        _database = client.GetDatabase(settings.DatabaseName);

        logger.LogInformation("Connected to MongoDB database '{DatabaseName}'.", settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName) => _database.GetCollection<T>(collectionName);
}
