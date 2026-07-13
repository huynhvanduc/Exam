using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.RoleAggregate;
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

        var categories = context.GetCollection<Category>("categories");
        var categoryUrlPathIndex = new CreateIndexModel<Category>(
            Builders<Category>.IndexKeys.Ascending(x => x.UrlPath),
            new CreateIndexOptions { Unique = true });

        await categories.Indexes.CreateOneAsync(categoryUrlPathIndex, cancellationToken: cancellationToken);

        var rolePermissions = context.GetCollection<RolePermissionSet>("rolePermissions");
        var rolePermissionRoleIndex = new CreateIndexModel<RolePermissionSet>(
            Builders<RolePermissionSet>.IndexKeys.Ascending(x => x.Role),
            new CreateIndexOptions { Unique = true });

        await rolePermissions.Indexes.CreateOneAsync(rolePermissionRoleIndex, cancellationToken: cancellationToken);

        var examResults = context.GetCollection<ExamResult>("examResults");
        var examResultExamIdIndex = new CreateIndexModel<ExamResult>(
            Builders<ExamResult>.IndexKeys.Ascending(x => x.ExamId));

        await examResults.Indexes.CreateOneAsync(examResultExamIdIndex, cancellationToken: cancellationToken);
    }
}
