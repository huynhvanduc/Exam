using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Contracts;
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

    public async Task<IReadOnlyCollection<ExamEntity>> GetByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting Exams by CategoryId {CategoryId}.", categoryId);
        return await Collection.Find(x => x.CategoryId == categoryId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExamEntity>> GetAvailableAsync(DateTime at, int skip, int take, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting available Exams at {At}, skip {Skip}, take {Take}.", at, skip, take);
        return await Collection.Find(AvailableFilter(at))
            .SortByDescending(x => x.DateCreated)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CountAvailableAsync(DateTime at, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Counting available Exams at {At}.", at);
        return await Collection.CountDocumentsAsync(AvailableFilter(at), cancellationToken: cancellationToken);
    }

    private static FilterDefinition<ExamEntity> AvailableFilter(DateTime at) =>
        Builders<ExamEntity>.Filter.Eq(x => x.Status, ExamStatus.Published) &
        (Builders<ExamEntity>.Filter.Eq(x => x.AvailableFrom, null) | Builders<ExamEntity>.Filter.Lte(x => x.AvailableFrom, at)) &
        (Builders<ExamEntity>.Filter.Eq(x => x.AvailableTo, null) | Builders<ExamEntity>.Filter.Gte(x => x.AvailableTo, at));

    public async Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Checking Exam existence by CategoryId {CategoryId}.", categoryId);
        return await Collection.Find(x => x.CategoryId == categoryId).AnyAsync(cancellationToken);
    }
}
