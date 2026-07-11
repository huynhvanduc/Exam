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
}
