using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class ExamRepository : MongoRepositoryBase<ExamEntity>, IExamRepository
{
    private const string CollectionName = "exams";
    private static readonly SortDefinition<ExamEntity> ByDateCreatedDesc = Builders<ExamEntity>.Sort.Descending(x => x.DateCreated);

    public ExamRepository(MongoDbContext context, ILogger<ExamRepository> logger, IMediator mediator)
        : base(context, CollectionName, logger, mediator)
    {
    }

    public Task<IReadOnlyCollection<ExamEntity>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(Builders<ExamEntity>.Filter.Eq(x => x.CategoryId, categoryId), ByDateCreatedDesc, skip, take, cancellationToken);

    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamEntity>.Filter.Eq(x => x.CategoryId, categoryId), cancellationToken);

    public Task<IReadOnlyCollection<ExamEntity>> GetAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, int skip, int take, CancellationToken cancellationToken = default)
    {
        var utcAt = at.Kind == DateTimeKind.Utc ? at : at.ToUniversalTime();
        return FindPagedAsync(AvailableForUserFilter(utcAt, classIds), ByDateCreatedDesc, skip, take, cancellationToken);
    }

    public Task<long> CountAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, CancellationToken cancellationToken = default)
    {
        var utcAt = at.Kind == DateTimeKind.Utc ? at : at.ToUniversalTime();
        return CountFilteredAsync(AvailableForUserFilter(utcAt, classIds), cancellationToken);
    }

    private static FilterDefinition<ExamEntity> AvailableForUserFilter(DateTime utcAt, IReadOnlyCollection<string> classIds)
    {
        var filterBuilder = Builders<ExamEntity>.Filter;

        return filterBuilder.Eq(x => x.Status, ExamStatus.Published) &

               (filterBuilder.Eq(x => x.AvailableFrom, null) | filterBuilder.Lte(x => x.AvailableFrom, utcAt)) &
               (filterBuilder.Eq(x => x.AvailableTo, null) | filterBuilder.Gte(x => x.AvailableTo, utcAt)) &
               (filterBuilder.Eq(x => x.AssignedClassIds, Array.Empty<string>()) |
                filterBuilder.Eq(x => x.AssignedClassIds, null) |
                filterBuilder.AnyIn(x => x.AssignedClassIds, classIds));
    }

    public async Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Checking Exam existence by CategoryId {CategoryId}.", categoryId);

        var filter = Builders<ExamEntity>.Filter.Eq(x => x.CategoryId, categoryId);
        return await Collection.Find(filter).AnyAsync(cancellationToken);
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamEntity>.Filter.Empty, cancellationToken);

    public Task<long> CountByStatusAsync(ExamStatus status, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamEntity>.Filter.Eq(x => x.Status, status), cancellationToken);
}