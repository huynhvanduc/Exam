using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class ExamResultRepository : MongoRepositoryBase<ExamResult>, IExamResultRepository
{
    public ExamResultRepository(MongoDbContext context, ILogger<ExamResultRepository> logger, IMediator mediator)
        : base(context, "examResults", logger, mediator)
    {
    }

    public async Task<ExamResult> GetInProgressAttemptAsync(string userId, string examId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting in-progress ExamResult by UserId {UserId} and ExamId {ExamId}.", userId, examId);
        return await Collection.Find(x => x.UserId == userId && x.ExamId == examId && !x.Finished)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExamResult>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting ExamResults by UserId {UserId}.", userId);
        return await Collection.Find(x => x.UserId == userId)
            .SortByDescending(x => x.ExamStartDate)
            .ToListAsync(cancellationToken);
    }
}
