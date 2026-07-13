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

    private static readonly SortDefinition<ExamEntity> ByDateCreatedDesc = Builders<ExamEntity>.Sort.Descending(x => x.DateCreated);

    public Task<IReadOnlyCollection<ExamEntity>> GetByCategoryAsync(string categoryId, int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(Builders<ExamEntity>.Filter.Eq(x => x.CategoryId, categoryId), ByDateCreatedDesc, skip, take, cancellationToken);

    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamEntity>.Filter.Eq(x => x.CategoryId, categoryId), cancellationToken);

    public Task<IReadOnlyCollection<ExamEntity>> GetAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, int skip, int take, CancellationToken cancellationToken = default) =>
        FindPagedAsync(AvailableForUserFilter(at, classIds), ByDateCreatedDesc, skip, take, cancellationToken);

    public Task<long> CountAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(AvailableForUserFilter(at, classIds), cancellationToken);

    private static FilterDefinition<ExamEntity> AvailableForUserFilter(DateTime at, IReadOnlyCollection<string> classIds) =>
        Builders<ExamEntity>.Filter.Eq(x => x.Status, ExamStatus.Published) &
        (Builders<ExamEntity>.Filter.Eq(x => x.AvailableFrom, null) | Builders<ExamEntity>.Filter.Lte(x => x.AvailableFrom, at)) &
        (Builders<ExamEntity>.Filter.Eq(x => x.AvailableTo, null) | Builders<ExamEntity>.Filter.Gte(x => x.AvailableTo, at)) &
        (Builders<ExamEntity>.Filter.Size(x => x.AssignedClassIds, 0) | Builders<ExamEntity>.Filter.AnyIn(x => x.AssignedClassIds, classIds));

    public async Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Checking Exam existence by CategoryId {CategoryId}.", categoryId);
        return await Collection.Find(x => x.CategoryId == categoryId).AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByQuestionIdAsync(string questionId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Checking Exam existence by QuestionId {QuestionId}.", questionId);
        return await Collection.Find(Builders<ExamEntity>.Filter.AnyEq(x => x.QuestionIds, questionId)).AnyAsync(cancellationToken);
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamEntity>.Filter.Empty, cancellationToken);

    public Task<long> CountByStatusAsync(ExamStatus status, CancellationToken cancellationToken = default) =>
        CountFilteredAsync(Builders<ExamEntity>.Filter.Eq(x => x.Status, status), cancellationToken);
}
