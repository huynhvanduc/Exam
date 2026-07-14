using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class QuestionRepository : MongoRepositoryBase<Question>, IQuestionRepository
{
    public QuestionRepository(MongoDbContext context, ILogger<QuestionRepository> logger, IMediator mediator)
        : base(context, "questions", logger, mediator)
    {
    }

    public async Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting Questions by CategoryId {CategoryId}.", categoryId);
        return await Collection.Find(x => x.CategoryId == categoryId).ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(Builders<Question>.Filter.Eq(x => x.CategoryId, categoryId), Builders<Question>.Sort.Descending(x => x.DateCreated), skip, take, cancellationToken);

    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<Question>.Filter.Eq(x => x.CategoryId, categoryId), cancellationToken);

    public Task<IReadOnlyCollection<Question>> GetByCategoryAsync(string categoryId, int skip, int take,
        Level? level, QuestionType? questionType, string? keyword, CancellationToken cancellationToken = default) =>
        FindPagedAsync(BuildFilter(categoryId, level, questionType, keyword), Builders<Question>.Sort.Descending(x => x.DateCreated), skip, take, cancellationToken);

    public Task<long> CountByCategoryAsync(string categoryId, Level? level, QuestionType? questionType, string? keyword,
        CancellationToken cancellationToken = default) =>
        CountFilteredAsync(BuildFilter(categoryId, level, questionType, keyword), cancellationToken);

    private static FilterDefinition<Question> BuildFilter(string categoryId, Level? level, QuestionType? questionType, string? keyword)
    {
        var filter = Builders<Question>.Filter.Eq(x => x.CategoryId, categoryId);

        if (level.HasValue)
            filter &= Builders<Question>.Filter.Eq(x => x.Level, level.Value);

        if (questionType.HasValue)
            filter &= Builders<Question>.Filter.Eq(x => x.QuestionType, questionType.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
            filter &= Builders<Question>.Filter.Regex(x => x.Content, new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(keyword), "i"));

        return filter;
    }

    public async Task<IReadOnlyCollection<Question>> GetByCategoryLevelTypeAsync(string categoryId, Level level,
        QuestionType questionType, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting Questions by CategoryId {CategoryId}, Level {Level}, Type {Type}.", categoryId, level, questionType);
        var filter = Builders<Question>.Filter.Eq(x => x.CategoryId, categoryId)
                     & Builders<Question>.Filter.Eq(x => x.Level, level)
                     & Builders<Question>.Filter.Eq(x => x.QuestionType, questionType);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Question>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids?.ToList() ?? new List<string>();
        Logger.LogDebug("Getting Questions by Ids {Ids}.", idList);
        return await Collection.Find(x => idList.Contains(x.Id)).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Checking Question existence by CategoryId {CategoryId}.", categoryId);
        return await Collection.Find(x => x.CategoryId == categoryId).AnyAsync(cancellationToken);
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<Question>.Filter.Empty, cancellationToken);
}
