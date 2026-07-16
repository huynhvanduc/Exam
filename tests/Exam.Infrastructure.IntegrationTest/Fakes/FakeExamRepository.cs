using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Infrastructure.IntegrationTest.Fakes;

public class FakeExamRepository : InMemoryRepositoryBase<Domain.AggregateModels.ExamAggregate.Exam>, IExamRepository
{
    public Task<IReadOnlyCollection<Domain.AggregateModels.ExamAggregate.Exam>> GetByCategoryAsync(string categoryId, int skip, int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Domain.AggregateModels.ExamAggregate.Exam>>(
            Items.Where(x => x.CategoryId == categoryId).Skip(skip).Take(take).ToList());

    public Task<long> CountByCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Items.Count(x => x.CategoryId == categoryId));

    public Task<IReadOnlyCollection<Domain.AggregateModels.ExamAggregate.Exam>> GetAvailableForUserAsync(DateTime at,
        IReadOnlyCollection<string> classIds, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Domain.AggregateModels.ExamAggregate.Exam>>(
            Items.Where(x => x.IsAvailable(at) && x.IsAssignedToAnyOf(classIds)).Skip(skip).Take(take).ToList());

    public Task<long> CountAvailableForUserAsync(DateTime at, IReadOnlyCollection<string> classIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Items.Count(x => x.IsAvailable(at) && x.IsAssignedToAnyOf(classIds)));

    public Task<bool> ExistsByCategoryIdAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(x => x.CategoryId == categoryId));

    public Task<long> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult((long)Items.Count);

    public Task<long> CountByStatusAsync(ExamStatus status, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Items.Count(x => x.Status == status));
}
