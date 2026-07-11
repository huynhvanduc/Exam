using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class ExamRepository : MongoRepositoryBase<ExamEntity>, IExamRepository
{
    public ExamRepository(MongoDbContext context, ILogger<ExamRepository> logger, IMediator mediator)
        : base(context, "exams", logger, mediator)
    {
    }

    public async Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Checking Exam existence by CategoryId {CategoryId}.", categoryId);
        return await Collection.Find(x => x.CategoryId == categoryId).AnyAsync(cancellationToken);
    }
}
