using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class MongoClientFactory
{
    public static IMongoClient Create(MongoDbSettings settings)
    {
        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.RetryReads = true;
        clientSettings.RetryWrites = true;

        return new MongoClient(clientSettings);
    }
}
