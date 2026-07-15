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

    public Task<IReadOnlyCollection<ExamResult>> GetByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(Builders<ExamResult>.Filter.Eq(x => x.UserId, userId), Builders<ExamResult>.Sort.Descending(x => x.ExamStartDate), skip, take, cancellationToken);

    public Task<long> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamResult>.Filter.Eq(x => x.UserId, userId), cancellationToken);

    public Task<IReadOnlyCollection<ExamResult>> GetByExamIdAsync(string examId, int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(Builders<ExamResult>.Filter.Eq(x => x.ExamId, examId), Builders<ExamResult>.Sort.Descending(x => x.ExamStartDate), skip, take, cancellationToken);

    public Task<long> CountByExamIdAsync(string examId, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamResult>.Filter.Eq(x => x.ExamId, examId), cancellationToken);

    public Task<long> CountByUserIdAndExamIdAsync(string userId, string examId, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(
            Builders<ExamResult>.Filter.Eq(x => x.UserId, userId) & Builders<ExamResult>.Filter.Eq(x => x.ExamId, examId),
            cancellationToken);

    public async Task<IReadOnlyCollection<ExamResult>> GetAllInProgressAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all in-progress ExamResults.");
        return await Collection.Find(x => !x.Finished).ToListAsync(cancellationToken);
    }
}
