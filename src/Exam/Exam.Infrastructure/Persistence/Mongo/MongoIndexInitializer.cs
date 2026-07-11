using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(MongoDbContext context, CancellationToken cancellationToken = default)
    {
        var users = context.GetCollection<User>("users");
        var userExternalIdIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.ExternalId),
            new CreateIndexOptions { Unique = true });

        await users.Indexes.CreateOneAsync(userExternalIdIndex, cancellationToken: cancellationToken);

        var questions = context.GetCollection<Question>("questions");
        var questionCategoryIndex = new CreateIndexModel<Question>(
            Builders<Question>.IndexKeys.Ascending(x => x.CategoryId));

        await questions.Indexes.CreateOneAsync(questionCategoryIndex, cancellationToken: cancellationToken);
    }
}
