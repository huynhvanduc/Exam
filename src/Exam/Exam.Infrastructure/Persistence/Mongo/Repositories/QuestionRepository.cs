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
}
